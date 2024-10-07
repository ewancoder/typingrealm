using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TypingRealm.Typing.DataAccess;

namespace TypingRealm.Typing.Api.Controllers;

public sealed record CharacterStatistics(
    int totalTyped,
    decimal totalTimePerf,
    IEnumerable<CharacterStats> Characters);
public sealed record CharacterStats(
    char Character,
    int TotalTyped,
    int TotalErrors)
{
    public decimal ErrorIndex => (decimal)TotalErrors / TotalTyped;
}
public sealed record Error(
    string sessionId,
    string word,
    string phrase,
    int characterIndex,
    int InWordIndex,
    char correctCharacter,
    char errorCharacter)
{
    public int CorrectionLetters { get; set; }
    public decimal CorrectionTimePerf { get; set; }
}
public sealed record SessionStatistics(
    int TextLength,
    decimal TimePerf,
    List<Error> Errors,
    List<char> keysTyped);

[Authorize]
[Route("api/statistics")]
public sealed class StatisticsController : ControllerBase
{
    // TODO: Introduce other languages and refactor this.
    private const string AllKeys = "1234567890[]`~!@#$%^&*(){}',.pyfgcrl/=\"<>PYFGCRL?+|aoeuidhtns-AOEUIDHTNS_;qjkxbmwvz:QJKXBMWVZ ";
    private readonly ITypingRepository _typingRepository;

    public StatisticsController(ITypingRepository typingRepository)
    {
        _typingRepository = typingRepository;
    }

    [HttpGet]
    [Route("profile")]
    public ValueTask<ProfileInfo> GetProfileInfo()
    {
        return _typingRepository.GetProfileInfoAsync();
    }

    [HttpPost]
    [Route("profile")]
    public async ValueTask SaveProfileInfo(ProfileInfo profileInfo)
    {
        await _typingRepository.SaveProfileInfoAsync(profileInfo);
    }

    [HttpGet]
    [Route("characters")]
    public async ValueTask<CharacterStatistics> CalculateCharacterStatistics(string orderBy)
    {
        var errorStatistics = await CalculateErrorsStatistics();

        var totalTyped = 0;
        var totalTimePerf = 0m;
        var totalKeysTyped = new List<char>();
        var statisticsErrors = new List<Error>();
        foreach (var statistics in errorStatistics)
        {
            totalTyped += statistics.TextLength;
            totalTimePerf += statistics.TimePerf;

            totalKeysTyped.AddRange(statistics.keysTyped);
            statisticsErrors.AddRange(statistics.Errors);
        }

        var charactersEnumerable = statisticsErrors.GroupBy(x => x.correctCharacter).Select(x => new CharacterStats(
            x.Key,
            totalKeysTyped.Count(o => o == x.Key),
            x.Count()));

        charactersEnumerable = orderBy == "typed"
            ? charactersEnumerable.OrderBy(x => x.TotalTyped)
            : (IEnumerable<CharacterStats>)charactersEnumerable.OrderByDescending(x => x.ErrorIndex);

        return new CharacterStatistics(totalTyped, totalTimePerf, charactersEnumerable.ToList());
    }

    [HttpGet]
    [Route("errors")]
    public async ValueTask<IEnumerable<SessionStatistics>> CalculateErrorsStatistics()
    {
        var statistics = new List<SessionStatistics>();
        foreach (var info in await _typingRepository.GetAllTypingSessionInfosAsync())
        {
            var typingResult = await _typingRepository.GetTypingResultByIdAsync(info.Id)
                ?? throw new InvalidOperationException("Could not load typing result.");

            // If text was custom generated - do not include into statistics.
            if (typingResult.TextMetadata?.Theme != null
                || typingResult.TextMetadata?.ForTraining != null)
                continue;

            statistics.Add(GetSessionStatistics(info.Id, typingResult));
        }

        return statistics;
    }

    private SessionStatistics GetSessionStatistics(string id, TypingResult result)
    {
        // TODO: Check session 58: it has bad letters at the end.
        // TODO: Also check that at the end "typed" part is equal to the "text".
        // For example, in that 58 bad session there's a discrepancy for some reason.
        //if (id == "58")

        var text = result.Text;
        var index = 0;
        var typed = string.Empty;
        var errorIndex = 0;
        var firstPerf = -1m;
        var lastPerf = -1m;
        var firstCorrectionPerf = 0m;
        var correctionLetters = 0;
        var keysTyped = new List<char>();
        var errors = new List<Error>();
        foreach (var e in result.Events)
        {
            try
            {
                // Only account Press events.
                if (e.KeyAction == KeyAction.Release)
                    continue;

                if (firstPerf == -1)
                    firstPerf = e.Perf;
                lastPerf = e.Perf;

                if (e.Key.ToLowerInvariant() == "backspace" && index > 0)
                {
                    if (errorIndex == index)
                    {
                        errorIndex = 0;
                        errors.Last().CorrectionLetters = correctionLetters;
                        errors.Last().CorrectionTimePerf = lastPerf - firstCorrectionPerf;
                    }

                    index--;
                    typed = typed[..^1];
                    continue;
                }

                if (e.Key.Length != 1)
                    continue;

                if (index < text.Length && e.Key[0] == text[index])
                {
                    typed += e.Key;
                    index++;
                    keysTyped.Add(e.Key[0]);

                    continue;
                }

                // If we got here - there's an error.
                typed += e.Key;
                index++;
                correctionLetters++;

                if (errorIndex == 0)
                {
                    correctionLetters = 1;
                    firstCorrectionPerf = lastPerf;
                    errorIndex = index;

                    var error = MakeError(id, text, index - 1, e.Key[0]);
                    errors.Add(error);
                }
            } catch (Exception exception)
            {
                // This is for debugging purposes.
                throw;
            }
        }

        return new SessionStatistics(result.Text.Length, lastPerf - firstPerf, errors, keysTyped);
    }

    private Error MakeError(string sessionId, string text, int index, char key)
    {
        var wordAndInWordIndex = GetWordAndInWordIndex(text, index);
        return new Error(sessionId, wordAndInWordIndex.Item1, GetPhrase(text, index), index, wordAndInWordIndex.Item2, text[index], key);
    }

    private Tuple<string, int> GetWordAndInWordIndex(string text, int index)
    {
        if (text.Length == index + 1)
        {
            // TODO: get last word.
            var firstIndex = text.Length - 1;
            while (text[firstIndex] != ' ')
                firstIndex--;

            return new(text[(firstIndex + 1)..], index - firstIndex - 1);
        }

        if (index == 0)
        {
            // TODO: Get first word.
            var lastIndex = 0;
            while (text[lastIndex] != ' ')
                lastIndex++;

            return new(text[..lastIndex], index);
        }

        {
            var firstIndex = index;
            var secondIndex = index;

            while (text[firstIndex] != ' ' && firstIndex > 0)
                firstIndex--;

            while (text[secondIndex] != ' ' && secondIndex < text.Length - 1)
                secondIndex++;

            if (firstIndex == secondIndex)
            {
                // Index was a space. Return two encompassing words.
                return new (GetPhrase(text, index), -1);
            }

            return new (text.Substring(firstIndex == 0 ? 0 : firstIndex + 1, secondIndex - firstIndex - 1), index - firstIndex - 1);
        }
    }

    private string GetPhrase(string text, int index)
    {
        var firstIndex = index;
        var secondIndex = index;

        if (text[index] == ' ')
        {
            if (firstIndex > 0) firstIndex--;
            if (secondIndex < text.Length - 1) secondIndex++;
        }

        while (text[firstIndex] != ' ' && firstIndex > 0)
            firstIndex--;

        while (text[secondIndex] != ' ' && secondIndex < text.Length - 1)
            secondIndex++;

        if (text.Substring(firstIndex == 0 ? 0 : firstIndex + 1, secondIndex - firstIndex - 1).Contains(' '))
            return text.Substring(firstIndex == 0 ? 0 : firstIndex + 1, secondIndex - firstIndex - 1);

        if (firstIndex > 0) firstIndex--;
        if (secondIndex < text.Length - 1) secondIndex++;

        while (text[firstIndex] != ' ' && firstIndex > 0)
            firstIndex--;

        while (text[secondIndex] != ' ' && secondIndex < text.Length - 1)
            secondIndex++;

        return text.Substring(firstIndex == 0 ? firstIndex : firstIndex + 1, secondIndex - firstIndex - 1);
    }
}
