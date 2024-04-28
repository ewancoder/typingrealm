using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TypingRealm.Framework;

namespace TypingRealm.Typing.DataAccess;

// TODO: Avoid primitive obsession using TypingSessionId and TypingResultId, and User/Profile.
// TODO: Consider refactoring User and Archived contexts to specifications.

public interface ITypingRepository
{
    ValueTask<IEnumerable<TypingSessionInfo>> GetAllTypingSessionInfosAsync();
    ValueTask<TypingResult?> GetTypingResultByIdAsync(string id);
    ValueTask<TypingSessionInfo> SaveTypingResultAsync(TypingResult typingResult);
    ValueTask<bool> ArchiveTypingSessionByIdAsync(string id);
    ValueTask<bool> RollbackArchiveTypingSessionByIdAsync(string id);
}

public sealed class TypingRepository : ITypingRepository
{
    private readonly IAuthenticationContext _authenticationContext;
    private readonly NpgsqlDataSource _db;

    public TypingRepository(
        IAuthenticationContext authenticationContext,
        IConfiguration configuration)
    {
        _authenticationContext = authenticationContext;

        var builder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("DataConnectionString"));
        _db = builder.Build();
    }

    public async ValueTask<bool> ArchiveTypingSessionByIdAsync(string id)
    {
        var profileId = _authenticationContext.GetUserProfileId();

        using var connection = await _db.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(@"UPDATE typing_bundle SET is_archived = true WHERE id = @id AND profile_id = @profileId AND is_archived = false", connection);
        cmd.Parameters.AddWithValue("@id", Convert.ToInt64(id));
        cmd.Parameters.AddWithValue("@profileId", profileId);

        var affectedRows = await cmd.ExecuteNonQueryAsync();

        return affectedRows > 0;
    }

    public async ValueTask<IEnumerable<TypingSessionInfo>> GetAllTypingSessionInfosAsync()
    {
        var profileId = _authenticationContext.GetUserProfileId();

        using var connection = await _db.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(@"SELECT id, text, started_typing_at, finished_typing_at FROM typing_bundle WHERE profile_id = @profileId AND is_archived = false ORDER BY started_typing_at DESC", connection);
        cmd.Parameters.AddWithValue("@profileId", profileId);

        var result = new List<TypingSessionInfo>();

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var startedTypingAt = reader.GetDateTime(2);
            var finishedTypingAt = reader.GetDateTime(3);
            var lengthSeconds = Convert.ToDecimal((finishedTypingAt - startedTypingAt).TotalSeconds);

            result.Add(new TypingSessionInfo(
                reader.GetInt64(0).ToString(),
                reader.GetString(1),
                startedTypingAt,
                lengthSeconds));
        }

        return result;
    }

    public async ValueTask<TypingResult?> GetTypingResultByIdAsync(string id)
    {
        var profileId = _authenticationContext.GetUserProfileId();

        using var connection = await _db.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(@"
SELECT text, started_typing_at, finished_typing_at, client_timezone, client_timezone_offset, events, theme, for_training
FROM typing_bundle
LEFT JOIN text_metadata
ON id = typing_bundle_id
WHERE id = @id AND profile_id = @profileId AND is_archived = false", connection);
        cmd.Parameters.AddWithValue("@id", Convert.ToInt64(id));
        cmd.Parameters.AddWithValue("@profileId", profileId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new TypingResult(
                reader.GetString(0),
                reader.GetDateTime(1),
                reader.GetDateTime(2),
                reader.GetString(3),
                reader.GetInt32(4),
                JsonSerializer.Deserialize<IEnumerable<TypingEvent>>(reader.GetString(5))
                    ?? throw new InvalidOperationException("Could not deserialize events."),
                CreateMetadata(reader));
        }

        return null;
    }

    public async ValueTask<bool> RollbackArchiveTypingSessionByIdAsync(string id)
    {
        var profileId = _authenticationContext.GetUserProfileId();

        using var connection = await _db.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(@"UPDATE typing_bundle SET is_archived = false WHERE id = @id AND profile_id = @profileId AND is_archived = true", connection);
        cmd.Parameters.AddWithValue("@id", Convert.ToInt64(id));
        cmd.Parameters.AddWithValue("@profileId", profileId);

        var affectedRows = await cmd.ExecuteNonQueryAsync();

        return affectedRows > 0;
    }

    public async ValueTask<TypingSessionInfo> SaveTypingResultAsync(TypingResult typingResult)
    {
        var profileId = _authenticationContext.GetUserProfileId();

        using var connection = await _db.OpenConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();

        string id;
        DateTime startedTypingAt;
        decimal lengthSeconds;
        {
            await using var cmd = new NpgsqlCommand(@"INSERT INTO typing_bundle (submitted_at, text, profile_id, started_typing_at, finished_typing_at, client_timezone, client_timezone_offset, events) VALUES (@submittedAt, @text, @profileId, @startedTypingAt, @finishedTypingAt, @clientTimezone, @clientTimezoneOffset, @events) RETURNING id", connection, transaction);

            cmd.Parameters.AddWithValue("submittedAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("text", typingResult.Text);
            cmd.Parameters.AddWithValue("profileId", profileId);
            cmd.Parameters.AddWithValue("startedTypingAt", typingResult.StartedTypingAt);
            cmd.Parameters.AddWithValue("finishedTypingAt", typingResult.FinishedTypingAt);
            cmd.Parameters.AddWithValue("clientTimezone", typingResult.Timezone);
            cmd.Parameters.AddWithValue("clientTimezoneOffset", typingResult.TimezoneOffset);

            var jsonEvents = cmd.Parameters.Add("events", NpgsqlTypes.NpgsqlDbType.Json);
            jsonEvents.Value = JsonSerializer.Serialize(typingResult.Events);

            var idObj = await cmd.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("Database insert returned null result.");
            id = idObj.ToString() ?? throw new InvalidOperationException("Database insert returned null result");

            startedTypingAt = typingResult.StartedTypingAt;
            var finishedTypingAt = typingResult.FinishedTypingAt;
            lengthSeconds = Convert.ToDecimal((finishedTypingAt - startedTypingAt).TotalSeconds);
        }

        if (typingResult.TextMetadata != null)
        {
            await using var cmd = new NpgsqlCommand(@"INSERT INTO text_metadata (typing_bundle_id, theme, for_training) VALUES (@typingBundleId, @theme, @forTraining)", connection, transaction);

            cmd.Parameters.AddWithValue("typingBundleId", id);
            cmd.Parameters.AddWithValue("theme", ((object)typingResult.TextMetadata.Theme!) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("forTraining", ((object)typingResult.TextMetadata.ForTraining!) ?? DBNull.Value);
        }

        await transaction.CommitAsync();

        return new TypingSessionInfo(
            id, typingResult.Text, startedTypingAt, lengthSeconds);
    }

    private TextMetadata? CreateMetadata(DbDataReader reader)
    {
        string? theme = null;
        string? forTraining = null;

        if (!reader.IsDBNull(6))
        {
            theme = reader.GetString(6);
        }

        if (!reader.IsDBNull(7))
        {
            forTraining = reader.GetString(7);
        }

        if (theme == null && forTraining == null)
            return null;

        return new TextMetadata(theme, forTraining);
    }
}
