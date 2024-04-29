using System;
using System.Collections.Generic;

namespace TypingRealm.Typing;

public sealed record TextMetadata(
    string? Theme,
    string? ForTraining);

public sealed record TypingResult(
    string Text,
    DateTime StartedTypingAt,
    DateTime FinishedTypingAt,
    string Timezone,
    int TimezoneOffset,
    IEnumerable<TypingEvent> Events,
    TextMetadata? TextMetadata);

public sealed record TypingEvent(
    string Key,
    decimal Perf,
    int Index,
    KeyAction KeyAction);

public enum KeyAction
{
    None = 0,
    Press = 1,
    Release
}
