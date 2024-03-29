﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TypingRealm.Authentication.Api;
using TypingRealm.Hosting;

namespace TypingRealm.Typing.Api.Controllers;

#pragma warning disable CS8618
public sealed class StartUserSessionDto
{
    public string TypingSessionId { get; set; }
    public int UserTimeZoneOffsetMinutes { get; set; }
}

public sealed class UserSessionDto
{
    public string UserSessionId { get; set; }
    public string TypingSessionId { get; set; }
}
#pragma warning restore CS8618

[UserScoped]
[Route("api/[controller]")]
public sealed class UserSessionsController : TyrController
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly ITypingSessionRepository _typingSessionRepository;
    private readonly ITypingResultProcessor _typingResultProcessor;
    private readonly ITypingReportGenerator _typingReportGenerator;

    public UserSessionsController(
        IUserSessionRepository userSessionRepository,
        ITypingSessionRepository typingSessionRepository,
        ITypingResultProcessor typingResultProcessor,
        ITypingReportGenerator typingReportGenerator)
    {
        _userSessionRepository = userSessionRepository;
        _typingSessionRepository = typingSessionRepository;
        _typingResultProcessor = typingResultProcessor;
        _typingReportGenerator = typingReportGenerator;
    }

    [HttpGet]
    [Route("{userSessionId}")]
    public async ValueTask<ActionResult<UserSessionDto>> GetById(string userSessionId)
    {
        var userSession = await _userSessionRepository.FindAsync(userSessionId);
        if (userSession == null)
            return NotFound();

        // TODO: Validate that user session belongs to this user.

        return Ok(new UserSessionDto
        {
            UserSessionId = userSessionId,
            TypingSessionId = userSession.TypingSessionId
        });
    }

    [HttpPost]
    public async ValueTask<ActionResult> StartUserSession(StartUserSessionDto dto)
    {
        var typingSession = await _typingSessionRepository.FindAsync(dto.TypingSessionId);
        if (typingSession == null)
            return NotFound();

        var userSessionId = await _userSessionRepository.NextIdAsync();

        var userSession = new UserSession(userSessionId, ProfileId, dto.TypingSessionId, DateTime.UtcNow, dto.UserTimeZoneOffsetMinutes);
        await _userSessionRepository.SaveAsync(userSession);

        var result = new { userSessionId };

        return CreatedAtAction(nameof(GetById), result, result);
    }

    [HttpPost]
    [Route("{userSessionId}/result")]
    public async ValueTask<ActionResult> SubmitTypingResult(string userSessionId, TextTypingResult textTypingResult)
    {
        // TODO: Use a separate DTO class, do not accept business entities here.

        var typingResult = textTypingResult with
        {
            TextTypingResultId = Guid.NewGuid().ToString(),
            SubmittedResultsUtc = DateTime.UtcNow
        };

        await _typingResultProcessor.AddTypingResultAsync(userSessionId, typingResult);

        var result = new { textTypingResultId = typingResult.TextTypingResultId };

        return CreatedAtAction(nameof(SubmitTypingResult), result, result);
    }

    // TODO: Consider moving to a separate controller.
    [HttpPost]
    [Route("result")]
    public async ValueTask<ActionResult> SubmitTypingResult(TypedText typedText)
    {
        // TODO: Use a separate DTO class, do not accept business entities here.

        var result = await _typingResultProcessor.AddTypingResultAsync(typedText, ProfileId);

        return CreatedAtAction(nameof(SubmitTypingResult), result, result);
    }

    [HttpGet]
    [Route("statistics")]
    public async ValueTask<ActionResult<TypingReport>> GetTypingReport(string? language = "en", TextGenerationType textGenerationType = TextGenerationType.GeneratedStardardText)
    {
        if (language == null)
            language = "en";

        var report = await _typingReportGenerator.GenerateReportAsync(ProfileId, language, textGenerationType);

        return Ok(report);
    }

    [HttpGet]
    [Route("{userSessionId}/statistics")]
    public async ValueTask<ActionResult<TypingReport>> GetTypingReportForUserSession(string userSessionId, TextGenerationType textGenerationType = TextGenerationType.GeneratedStardardText)
    {
        var report = await _typingReportGenerator.GenerateReportForUserSessionAsync(userSessionId, textGenerationType);

        return Ok(report);
    }

    [HttpGet]
    [Route("statistics/readable")]
    public async ValueTask<ActionResult<string>> GetReadableStatistics(string? language = "en")
    {
        if (language == null)
            language = "en";

        var response = await _typingReportGenerator.GenerateStandardHumanReadableReportAsync(ProfileId, language);

        return Ok(response);
    }
}
