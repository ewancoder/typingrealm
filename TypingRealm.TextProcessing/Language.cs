﻿namespace TypingRealm.TextProcessing;

public sealed class Language : Identity
{
    public Language(string value) : base(value)
    {
        Validation.ValidateIn(value, TextConstants.SupportedLanguageValues);
    }
}
