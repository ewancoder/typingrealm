using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace TypingRealm.Typing.Api.Controllers;

[Authorize]
[Route("api/typing")]
public sealed class TypingController : ControllerBase
{
    private readonly NpgsqlDataSource _db;

    public TypingController(
        IConfiguration configuration,
        Dictionary<string, TypingResultDao> data)
    {
        var builder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("DataConnectionString"));
        _db = builder.Build();
    }

    [HttpGet]
    public async Task<ActionResult<TypingResult>> GetTypingResultById(string id)
    {
        var profileId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

        using var connection = await _db.OpenConnectionAsync();

        // TODO: Avoid injection attacks.
        await using (var cmd = new NpgsqlCommand(@"SELECT text, started_typing_at, finished_typing_at, client_timezone, client_timezone_offset, events FROM typing_bundle WHERE id = @id AND profile_id = @profileId", connection))
        {
            cmd.Parameters.AddWithValue("@id", Convert.ToInt64(id));
            cmd.Parameters.AddWithValue("@profileId", profileId);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return new TypingResult(
                        reader.GetString(0),
                        reader.GetDateTime(1),
                        reader.GetDateTime(2),
                        reader.GetString(3),
                        reader.GetInt32(4),
                        JsonSerializer.Deserialize<IEnumerable<TypingEvent>>(reader.GetString(5)));
                }
            }
        }

        return NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<TypingResult>> LogTypingResult(TypingResult typingResult)
    {
        var profileId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

        using var connection = await _db.OpenConnectionAsync();

        await using (var cmd = new NpgsqlCommand(@"INSERT INTO typing_bundle (submitted_at, text, profile_id, started_typing_at, finished_typing_at, client_timezone, client_timezone_offset, events) VALUES (@submittedAt, @text, @profileId, @startedTypingAt, @finishedTypingAt, @clientTimezone, @clientTimezoneOffset, @events) RETURNING id", connection))
        {
            cmd.Parameters.AddWithValue("submittedAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("text", typingResult.Text);
            cmd.Parameters.AddWithValue("profileId", profileId);
            cmd.Parameters.AddWithValue("startedTypingAt", typingResult.StartedTypingAt);
            cmd.Parameters.AddWithValue("finishedTypingAt", typingResult.FinishedTypingAt);
            cmd.Parameters.AddWithValue("clientTimezone", typingResult.Timezone);
            cmd.Parameters.AddWithValue("clientTimezoneOffset", typingResult.TimezoneOffset);

            var jsonEvents = cmd.Parameters.Add("events", NpgsqlTypes.NpgsqlDbType.Json);
            jsonEvents.Value = JsonSerializer.Serialize(typingResult.Events);

            var id = await cmd.ExecuteScalarAsync();

            return CreatedAtAction(nameof(GetTypingResultById), new { id }, typingResult);
        }
    }
}
