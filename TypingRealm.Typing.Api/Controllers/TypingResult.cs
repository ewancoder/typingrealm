using System.Collections.Generic;

namespace TypingRealm.Typing.Api.Controllers;

public sealed record TypingResultDao(
    string userId,
    TypingResult result);

public sealed record TypingResult(
    string Text,
    IEnumerable<TypingEvent> Events);

public sealed record TypingEvent(
    string Key,
    decimal Perf,
    int Index,
    KeyAction KeyAction);

public enum KeyAction
{
    Press = 1,
    Release
}
