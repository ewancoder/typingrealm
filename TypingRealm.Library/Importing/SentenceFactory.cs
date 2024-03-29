﻿using System;
using System.Collections.Generic;
using System.Linq;
using TypingRealm.Library.Books;
using TypingRealm.Library.Sentences;
using TypingRealm.TextProcessing;

namespace TypingRealm.Library.Importing;

public interface ISentenceFactory
{
    Sentence CreateSentence(BookId bookId, string validatedSentence, int indexInBook);
}

public sealed class SentenceFactory : ISentenceFactory
{
    private readonly ITextProcessor _textProcessor;

    public SentenceFactory(ITextProcessor textProcessor)
    {
        _textProcessor = textProcessor;
    }

    public Sentence CreateSentence(BookId bookId, string validatedSentence, int indexInBook)
    {
        validatedSentence = string.Join(" ", _textProcessor.GetWordsEnumerable(validatedSentence));
        var sentenceType = SentenceType.Normal;

        var sentenceId = SentenceId.New();
        var words = _textProcessor.GetWordsEnumerable(validatedSentence).ToArray();
        var wordsList = new List<Word>(words.Length);

        var wordsInSentence = words
            .GroupBy(word => word)
            .Select(group => new
            {
                Word = group.Key,
                Count = group.Count()
            }).ToDictionary(x => x.Word);

        foreach (var word in words)
        {
            if (word.All(character => TextConstants.PunctuationCharacters.Contains(character))
                && word.Length > 1)
                sentenceType = SentenceType.Other;

            if (word.EndsWith(".?", StringComparison.Ordinal) || word.EndsWith(".!", StringComparison.Ordinal))
                sentenceType = SentenceType.Other;
        }

        var rawWordsInSentence = words
            .Select(word => _textProcessor.NormalizeWord(word))
            .GroupBy(word => word)
            .Select(group => new
            {
                Word = group.Key,
                Count = group.Count()
            }).ToDictionary(x => x.Word);

        var index = 0;
        foreach (var word in words)
        {
            var keyPairs = CreateKeyPairs(validatedSentence, word);

            var rawWord = _textProcessor.NormalizeWord(word);
            wordsList.Add(new Word(
                sentenceId, index,
                word, rawWord,
                wordsInSentence[word].Count, rawWord == string.Empty ? 0 : rawWordsInSentence[rawWord].Count,
                keyPairs));
            index++;
        }

        return new Sentence(bookId, sentenceId, sentenceType, indexInBook, validatedSentence, wordsList);
    }

    private static IList<KeyPair> CreateKeyPairs(string sentence, string word)
    {
        // TODO: Measure performance here (ToDictionary).
        var keyPairsInSentence = GetKeyPairsEnumerable(sentence)
            .GroupBy(keyPair => keyPair.KeyPair)
            .Select(group => new
            {
                KeyPair = group.Key,
                Count = group.Count()
            }).ToDictionary(x => x.KeyPair);

        var keyPairs = GetKeyPairsEnumerable(word).ToList();

        var keyPairsInWord = keyPairs
            .GroupBy(keyPair => keyPair.KeyPair)
            .Select(group => new
            {
                KeyPair = group.Key,
                Count = group.Count()
            }).ToDictionary(x => x.KeyPair);

        return keyPairs.Select(x => new KeyPair(
            x.Index,
            x.KeyPair,
            keyPairsInWord[x.KeyPair].Count,
            keyPairsInSentence[x.KeyPair].Count)).ToList();
    }

    private sealed record KeyPairInText(int Index, string KeyPair);
    private static IEnumerable<KeyPairInText> GetKeyPairsEnumerable(string value)
    {
        if (value.Length == 0)
            throw new ArgumentException("Value should not be empty.", nameof(value));

        yield return new KeyPairInText(-1, $" {value[0]}");

        if (value.Length > 1)
            yield return new KeyPairInText(-1, $" {value[..2]}");

        var index = 0;
        while (index < value.Length - 1)
        {
            yield return new KeyPairInText(index, value.Substring(index, 2));

            if (value[index..].Length > 2)
                yield return new KeyPairInText(index, value.Substring(index, 3));
            else
                yield return new KeyPairInText(index, $"{value.Substring(index, 2)} ");

            index++;
        }

        yield return new KeyPairInText(index, $"{value[^1]} ");
    }
}
