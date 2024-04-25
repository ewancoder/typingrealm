using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TypingRealm.Typing.DataAccess;

namespace TypingRealm.Typing.Api.Controllers;

public sealed record TypingStatistics(
    decimal minSpeed,
    decimal maxSpeed,
    decimal averageSpeed,
    IEnumerable<KeyValuePair<string, string[]>> mistakes);

[Authorize]
[Route("api/typing")]
public sealed class TypingController : ControllerBase
{
    private readonly ITypingRepository _typingRepository;

    public TypingController(ITypingRepository typingRepository)
    {
        _typingRepository = typingRepository;
    }

    // This will be refactored later so that the statistics is re-calculating every time new result is pushed, and there's no need to re-iterate over all user texts.
    [HttpGet]
    [Route("statistics")]
    public async ValueTask<TypingStatistics> CalculateTypingStatistics()
    {
        var errors = new List<string>();
        var totalCharacters = 0m;
        var totalTime = 0m;
        var minSpeed = 0m;
        var maxSpeed = 0m;

        foreach (var info in await _typingRepository.GetAllTypingSessionInfosAsync())
        {
            var typing = await _typingRepository.GetTypingResultByIdAsync(info.Id)
                ?? throw new InvalidOperationException("Could not load typing result.");

            var text = typing.Text;
            var index = 0;
            var typed = string.Empty;
            var errorIndex = 0;

            decimal firstPerf = 0;
            decimal lastPerf = 0;
            foreach (var e in typing.Events)
            {
                if (e.KeyAction == KeyAction.Release)
                    continue;

                if (firstPerf == 0)
                    firstPerf = e.Perf;
                lastPerf = e.Perf;

                if (e.Key.ToLowerInvariant() == "backspace" && index > 0)
                {
                    if (errorIndex == index)
                        errorIndex = 0;

                    index--;
                    typed = typed[..^1];
                    continue;
                }

                if (e.Key.Length != 1)
                    continue;

                if (e.Key[0] == text[index])
                {
                    typed += e.Key;
                    index++;
                    continue;
                }

                // If we got here - there's an error.
                typed += e.Key;
                index++;

                if (errorIndex == 0)
                {
                    errorIndex = index;
                    if (typed.Length > 1)
                        errors.Add($"{typed[typed.Length - 2]}{text[index - 1]}");
                    else
                        errors.Add(text[0].ToString());
                }
            }

            var speed = text.Length / ((lastPerf - firstPerf) / 60000m);
            minSpeed = minSpeed == 0 ? speed : Math.Min(minSpeed, speed);
            maxSpeed = Math.Max(maxSpeed, speed);
            totalCharacters += text.Length;
            totalTime += lastPerf - firstPerf;

            if (typed != text)
                throw new InvalidOperationException("Could not successfully parse typing session to the end.");
        }

        var pairs = errors.GroupBy(x => x.Length == 1 ? x : x[1..])
            .OrderByDescending(x => x.Count())
            .Select(x => new KeyValuePair<string, string[]>(x.Key, x.ToArray()))
            .ToList();

        var averageSpeed = totalCharacters / (totalTime / 60000m);

        return new TypingStatistics(minSpeed, maxSpeed, averageSpeed, pairs);
    }

    [HttpGet]
    public ValueTask<IEnumerable<TypingSessionInfo>> GetAllTypingSessionInfos()
    {
        return _typingRepository.GetAllTypingSessionInfosAsync();
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult<TypingResult>> GetTypingResultById(string id)
    {
        var result = await _typingRepository.GetTypingResultByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TypingSessionInfo>> LogTypingResult(TypingResult typingResult)
    {
        // Validate inputs against attacks:
        if (typingResult.Text.Length > 100_000)
            return BadRequest("Text cannot be longer than 100.000 characters.");

        if (typingResult.Timezone.Length > 1000)
            return BadRequest("Timezone cannot be longer than 1000 characters.");

        if (typingResult.Events.Any(x => x.Key.Length > 1000))
            return BadRequest("Event keys value cannot be longer than 1000 characters.");

        var info = await _typingRepository.SaveTypingResultAsync(typingResult);
        return CreatedAtAction(nameof(GetTypingResultById), new { info.Id }, info);
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<ActionResult> ArchiveTypingSessionById(string id)
    {
        if (!await _typingRepository.ArchiveTypingSessionByIdAsync(id))
            return NotFound();

        return Ok();
    }

    [HttpPost]
    [Route("{id}/rollback-archive")]
    public async Task<ActionResult> RollbackArchiveTypingSessionById(string id)
    {
        if (!await _typingRepository.RollbackArchiveTypingSessionByIdAsync(id))
            return NotFound();

        return Ok();
    }
}
