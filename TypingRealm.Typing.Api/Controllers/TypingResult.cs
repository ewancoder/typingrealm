using System;
using System.Collections.Generic;

namespace TypingRealm.Typing.Api.Controllers;

public sealed record TypingResultDao(
    string userId,
    TypingResult result);
