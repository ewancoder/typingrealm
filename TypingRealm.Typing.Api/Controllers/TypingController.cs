using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace TypingRealm.Typing.Api.Controllers;

[Authorize]
[Route("api/typing")]
public sealed class TypingController : ControllerBase
{
    private readonly Dictionary<string, TypingResultDao> _data;

    public TypingController(Dictionary<string, TypingResultDao> data)
    {
        _data = data;
    }

    [HttpGet]
    public ActionResult<TypingResult> GetTypingResultById(string id)
    {
        if (!_data.ContainsKey(id))
            return NotFound();

        return Ok(_data[id]);
    }

    [HttpPost]
    public ActionResult<TypingResult> LogTypingResult(TypingResult typingResult)
    {
        var id = Guid.NewGuid().ToString();
        var user = User;

        _data.Add(id, new TypingResultDao($"g_{user.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value}", typingResult));

        return CreatedAtAction(nameof(GetTypingResultById), new { id }, typingResult);
    }
}
