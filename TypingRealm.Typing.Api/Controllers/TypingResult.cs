﻿using System;
using System.Collections.Generic;

namespace TypingRealm.Typing.Api.Controllers;

public sealed record TypingResultDao(
    string userId,
    TypingResult result);

public sealed record TypingResult(
    string Text,
    DateTime StartedTypingAt,
    DateTime FinishedTypingAt,
    string Timezone,
    int TimezoneOffset,
    IEnumerable<TypingEvent> Events);

public sealed record TypingEvent(
    string Key,
    decimal Perf,
    int Index,
    KeyAction KeyAction);

public sealed record TypingSessionInfo(
    string Id,
    string Text,
    DateTime StartedTypingAt,
    decimal LengthSeconds);

public enum KeyAction
{
    None = 0,
    Press = 1,
    Release
}
