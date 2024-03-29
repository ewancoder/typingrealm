﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TypingRealm.Communication;
using TypingRealm.Library.Sentences;
using TypingRealm.TextProcessing;

namespace TypingRealm.Texts;

public interface ITextGenerator
{
    ValueTask<GeneratedText> GenerateTextAsync(TextGenerationConfiguration configuration);
}

// TODO: Update interface definition so that it adheres to the rules of Texts domain, not Library domain.
// Also domain shouldn't care about EndpointAuthentication, remove it from here, now it's proper DI.
public interface ISentencesClient
{
    ValueTask<IEnumerable<SentenceDto>> GetSentencesAsync(
        SentencesRequest request,
        string language, // TODO: Use default "en" value.
        EndpointAuthentication? authentication = null,
        CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<string>> GetWordsAsync(
        WordsRequest request,
        string language, // TODO: Use default "en" value.
        EndpointAuthentication? authentication = null,
        CancellationToken cancellationToken = default);
}

public sealed class SentencesClient : ISentencesClient
{
    public static readonly string ServiceName = Library.ServiceConfiguration.ServiceName;
    public static readonly string RoutePrefix = Library.ServiceConfiguration.SentencesApiPrefix;
    private readonly IServiceClient _serviceClient;

    public SentencesClient(IServiceClient serviceClient)
    {
        _serviceClient = serviceClient;
    }

    public ValueTask<IEnumerable<SentenceDto>> GetSentencesAsync(
        SentencesRequest request,
        string language,
        EndpointAuthentication? authentication = null,
        CancellationToken cancellationToken = default)
    {
        if (authentication == null)
            authentication = EndpointAuthentication.Service;

        return _serviceClient.PostAsync<SentencesRequest, IEnumerable<SentenceDto>>(
            ServiceName,
            $"{RoutePrefix}?language={language}",
            authentication,
            request,
            cancellationToken);
    }

    public ValueTask<IEnumerable<string>> GetWordsAsync(WordsRequest request, string language, EndpointAuthentication? authentication = null, CancellationToken cancellationToken = default)
    {
        if (authentication == null)
            authentication = EndpointAuthentication.Service;

        return _serviceClient.PostAsync<WordsRequest, IEnumerable<string>>(
            ServiceName,
            $"{RoutePrefix}/words?language={language}",
            authentication,
            request,
            cancellationToken);
    }
}

public sealed class TextGenerator : ITextGenerator
{
    private const int MaxAllowedTextLength = 1000;

    private readonly ISentencesClient _libraryClient;

    public TextGenerator(ISentencesClient libraryClient)
    {
        _libraryClient = libraryClient;
    }

    public async ValueTask<GeneratedText> GenerateTextAsync(TextGenerationConfiguration configuration)
    {
        var count = 1000;
        var customQuerySuccess = true;

        IEnumerable<string> data;
        if (configuration.TextStructure == TextStructure.Words)
        {
            var wordsRequest = configuration.ShouldContain.Any()
                ? WordsRequest.ContainingKeyPairs(configuration.ShouldContain, count)
                : WordsRequest.Random(count);

            data = (await _libraryClient.GetWordsAsync(wordsRequest, configuration.Language)
                .ConfigureAwait(false))
                .Where(x => !string.IsNullOrWhiteSpace(x.Trim()));
        }
        else
        {
            var sentencesRequest = configuration.ShouldContain.Any()
                ? SentencesRequest.ContainingKeyPairs(configuration.ShouldContain, count)
                : SentencesRequest.Random(count, count);

            data = (await _libraryClient.GetSentencesAsync(sentencesRequest, configuration.Language)
                .ConfigureAwait(false))
                .Select(dto => dto.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x.Trim()));
        }

        if (!data.Any())
        {
            // If no specific data found - find any random sentences.
            customQuerySuccess = false;

            var request = SentencesRequest.Random(count);

            data = (await _libraryClient.GetSentencesAsync(request, configuration.Language)
                .ConfigureAwait(false))
                .Select(dto => dto.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x.Trim()));
        }

        if (!data.Any())
            throw new InvalidOperationException("There's no text data in Library service.");

        var dataList = data.ToList();
        var builder = new StringBuilder();

        var mustLength = Math.Min(configuration.MinimumLength, MaxAllowedTextLength);
        while (builder.Length < mustLength)
        {
            // TODO: Use StringBuilder for transformations.
            var randomTextPart = dataList[RandomNumberGenerator.GetInt32(0, dataList.Count)];

            if (configuration.IsLowerCase)
                randomTextPart = randomTextPart.ToLowerInvariant();

            if (configuration.StripPunctuation)
            {
                foreach (var character in TextConstants.PunctuationCharacters)
                {
                    randomTextPart = randomTextPart.Replace(character.ToString(), "");
                }
            }

            if (configuration.StripNumbers)
            {
                foreach (var character in TextConstants.NumberCharacters)
                {
                    randomTextPart = randomTextPart.Replace(character.ToString(), "");
                }
            }

            builder.Append($"{randomTextPart} ");

            if (builder.Length >= mustLength)
                break;
        }

        if (configuration.CutLastSentence && builder.Length > mustLength)
            builder.Remove(mustLength, builder.Length - mustLength);

        if (builder[^1] == TextConstants.SpaceCharacter)
            builder.Remove(builder.Length - 1, 1);

        return new GeneratedText(builder.ToString(), customQuerySuccess);
    }
}
