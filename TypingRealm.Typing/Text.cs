﻿using System;
using System.Collections.Generic;
using TypingRealm.Typing.Framework;

namespace TypingRealm.Typing;

public sealed record TextGenerationConfiguration(
    int Length,
    IEnumerable<string> ShouldContain,
    TextGenerationType TextGenerationType);

public enum TextGenerationType
{
    Unspecified = 0,

    GeneratedStardardText = 1,
    GeneratedStandardWords = 2,
    GeneratedSelfImprovementText = 3,
    GeneratedSelfImprovementWords = 4,
    Custom = 5,

    /// <summary>
    /// User posted text.
    /// </summary>
    NotGenerated = 10
}

public static class TextGenerationConfigurationExtensions
{
    public static TextGenerationType GetTextGenerationType(this Texts.TextGenerationConfiguration config)
    {
        if (config.StatisticsType == Texts.StatisticsType.Standard)
        {
            if (config.TextStructure == Texts.TextStructure.Text)
            {
                if (config.IsSelfImprovement)
                    return TextGenerationType.GeneratedSelfImprovementText;

                return TextGenerationType.GeneratedStardardText;
            }

            if (config.IsSelfImprovement)
                return TextGenerationType.GeneratedSelfImprovementWords;

            return TextGenerationType.GeneratedStandardWords;
        }

        return TextGenerationType.Custom;
    }
}

public enum TextType
{
    Unspecified = 0,

    Generated = 1,
    User = 2
}

public sealed record TextConfiguration(
    TextType TextType,
    TextGenerationConfiguration? TextGenerationConfiguration,
    string Language);

/// <summary>
/// Aggregate root. User-defined text or randomly generated one. We can reuse
/// the same text only if we are sure it will never change. Either we don't
/// allow to change the value of the text, or we need to generate new texts
/// every time.
/// </summary>
public sealed class Text : IIdentifiable
{
    public sealed record State(
        string TextId,
        string Value,
        string CreatedByUser,
        DateTime CreatedUtc,
        bool IsPublic,
        bool IsArchived,
        TextConfiguration Configuration) : IIdentifiable
    {
        string IIdentifiable.Id => TextId;
    }

    private State _state;

    public Text(string textId, string value, string createdByUser, DateTime createdUtc, bool isPublic, TextConfiguration configuration)
    {
        // TODO: Validate.

        if (configuration.TextType == TextType.Generated && configuration.TextGenerationConfiguration == null)
            throw new InvalidOperationException("Text generation configuration cannot be null when text type is Generated.");

        _state = new State(textId, value, createdByUser, createdUtc, isPublic, false, configuration);
    }

    string IIdentifiable.Id => _state.TextId;

    #region State

    private Text(State state)
    {
        // TODO: Validate.

        _state = state with { };
    }

    public static Text FromState(State state) => new Text(state);

    public State GetState() => _state with { };

    #endregion

    public string Value => _state.Value;
    public bool IsArchived => _state.IsArchived;
    public string Language => _state.Configuration.Language;

    public TextGenerationType TextGenerationType => _state.Configuration.TextGenerationConfiguration?.TextGenerationType ?? TextGenerationType.NotGenerated;

    public void MakePublic()
    {
        if (_state.IsArchived)
            throw new InvalidOperationException("The text is archived.");

        if (_state.IsPublic)
            throw new InvalidOperationException("Text is already public.");

        _state = _state with { IsPublic = true };
    }

    public void MakePrivate()
    {
        if (_state.IsArchived)
            throw new InvalidOperationException("The text is archived.");

        if (!_state.IsPublic)
            throw new InvalidOperationException("Test is already private.");

        _state = _state with { IsPublic = false };
    }

    public void Archive()
    {
        if (_state.IsPublic)
            throw new InvalidOperationException("Cannot archive public text.");

        _state = _state with { IsArchived = true };
    }
}
