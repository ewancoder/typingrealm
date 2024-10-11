using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TypingRealm.Game.DataAccess;

[Index(nameof(ProfileId))]
public class Character
{
    [StringLength(50)]
    public required string Id { get; init; }

    [StringLength(100)]
    public required string Name { get; init; }

    [StringLength(500)]
    public required string ProfileId { get; init; }

    [Range(0, 100)]
    public required int Level { get; init; }

    [Range(0, long.MaxValue)]
    public long Experience { get; init; }
}
