namespace TypingRealm.Typing;

public sealed record TypingSessionInfo(
    string Id,
    string Text,
    DateTime StartedTypingAt,
    decimal LengthSeconds);
