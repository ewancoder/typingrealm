using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TypingRealm.Game.DataAccess;

public sealed record CreateCharacterDto(string Name);

public sealed record UpdateCharacterDto(
    int Level, long Experience);

[Index(nameof(ProfileId))]
public class Character
{
    [StringLength(50)]
    public string Id { get; private set; } = null!;

    [StringLength(100)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string ProfileId { get; private set; } = null!;

    [Range(0, 100)]
    public required int Level { get; set; }

    [Range(0, long.MaxValue)]
    public required long Experience { get; set; }

    public static Character CreateNew(CreateCharacterDto dto, string profileId)
    {
        return new Character
        {
            Name = dto.Name,
            ProfileId = profileId,
            Level = 0,
            Experience = 0
        };
    }
}
