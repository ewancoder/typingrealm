using Microsoft.EntityFrameworkCore;
using TypingRealm.Framework;

namespace TypingRealm.Game.DataAccess;

public record CharacterInfo(string CharacterId, string Name, int Level, long Experience);

public sealed class CharacterRepository
{
    private readonly IAuthenticationContext _authenticationContext;
    private readonly GameDbContext _dbContext;

    public CharacterRepository(
        IAuthenticationContext authenticationContext,
        GameDbContext dbContext)
    {
        _authenticationContext = authenticationContext;
        _dbContext = dbContext;
    }

    public async ValueTask<CharacterInfo> GetCurrentCharacterInfoAsync()
    {
        // TODO: Consider moving this out on a level above. Repositories probably shouldn't know about it.
        var profileId = _authenticationContext.GetUserProfileId();

        var character = await _dbContext.Character
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ProfileId == profileId)
            ?? throw new InvalidOperationException("Could not find the character for the current profile.");

        return new(
            character.Id,
            character.Name,
            character.Level,
            character.Experience);
    }

    public async ValueTask CreateCharacterAsync(CreateCharacterDto createCharacter)
    {
        var profileId = _authenticationContext.GetUserProfileId();
        var character = Character.CreateNew(createCharacter, profileId);

        await _dbContext.Character.AddAsync(character);
        await _dbContext.SaveChangesAsync();
    }

    public async ValueTask UpdateCharacter(UpdateCharacterDto updateCharacter)
    {
        var profileId = _authenticationContext.GetUserProfileId();

        var character = await _dbContext.Character
            .FirstOrDefaultAsync(c => c.ProfileId == profileId)
            ?? throw new InvalidOperationException("Could not find the character for the current profile.");

        character.Experience = updateCharacter.Experience;
        character.Level = updateCharacter.Level;

        await _dbContext.SaveChangesAsync();
    }
}
