using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TypingRealm.Typing.DataAccess;

namespace TypingRealm.Typing.Api.Controllers;

[Authorize]
[Route("api/typing")]
public sealed class TypingController : ControllerBase
{
    private readonly ITypingRepository _typingRepository;

    public TypingController(ITypingRepository typingRepository)
    {
        _typingRepository = typingRepository;
    }

    [HttpGet]
    public ValueTask<IEnumerable<TypingSessionInfo>> GetAllTypingSessionInfos()
    {
        return _typingRepository.GetAllTypingSessionInfosAsync();
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult<TypingResult>> GetTypingResultById(string id)
    {
        var result = await _typingRepository.GetTypingResultByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TypingSessionInfo>> LogTypingResult(TypingResult typingResult)
    {
        // Validate inputs against attacks:
        if (typingResult.Text.Length > 100_000)
            return BadRequest("Text cannot be longer than 100.000 characters.");

        if (typingResult.Timezone.Length > 1000)
            return BadRequest("Timezone cannot be longer than 1000 characters.");

        if (typingResult.Events.Any(x => x.Key.Length > 1000))
            return BadRequest("Event keys value cannot be longer than 1000 characters.");

        var info = await _typingRepository.SaveTypingResultAsync(typingResult);
        return CreatedAtAction(nameof(GetTypingResultById), new { info.Id }, info);
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<ActionResult> ArchiveTypingSessionById(string id)
    {
        if (!await _typingRepository.ArchiveTypingSessionByIdAsync(id))
            return NotFound();

        return Ok();
    }

    [HttpPost]
    [Route("{id}/rollback-archive")]
    public async Task<ActionResult> RollbackArchiveTypingSessionById(string id)
    {
        if (!await _typingRepository.RollbackArchiveTypingSessionByIdAsync(id))
            return NotFound();

        return Ok();
    }
}
