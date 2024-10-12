using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TypingRealm.Game.DataAccess;

public sealed record CreateCharacterDto(string Name);

public sealed record UpdateCharacterDto(
    int Level, long Experience);

[Index(nameof(ProfileId))]
public sealed class Character
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

    public string LocationId { get; set; } = null!;
    public Location Location { get; set; } = null!;

    public MovementProgress? MovementProgress { get; set; }

    public static Character CreateNew(CreateCharacterDto dto, string profileId)
    {
        return new Character
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            ProfileId = profileId,
            Level = 0,
            Experience = 0,
            LocationId = DataAccess.LocationId.Start
        };
    }
}

public abstract class WorldUnit
{
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(10_000)]
    public string Description { get; set; } = null!;

    public IEnumerable<Asset> Assets { get; set; } = null!;
}

public sealed class Location : WorldUnit
{
    [StringLength(50)]
    public string Id { get; set; } = null!;

    [InverseProperty(nameof(LocationPath.FromLocation))]
    public ICollection<LocationPath> Paths { get; set; } = null!;

    [InverseProperty(nameof(LocationPath.ToLocation))]
    public ICollection<LocationPath> InversePaths { get; set; } = null!;
}

public sealed record LocationId(string value)
{
    public static readonly string Start = "start";
}

public sealed class Asset
{
    [StringLength(50)]
    public string Id { get; private set; } = null!;

    public AssetType Type { get; set; }

    // TODO: Do not store data here, just save a unique id and serve it from somewhere.
    public byte[] Data { get; set; } = null!;

    // For future sorting in the editor.
    [StringLength(500)]
    public string Path { get; set; } = null!;
}

public enum AssetType
{
    Unknown,
    Image
}

public sealed class LocationPath : WorldUnit
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; private set; }

    [ForeignKey(nameof(FromLocation))]
    public string FromLocationId { get; set; } = null!;
    public Location FromLocation { get; set; } = null!;

    [ForeignKey(nameof(ToLocation))]
    public string ToLocationId { get; set; } = null!;
    public Location ToLocation { get; set; } = null!;

    public long DistanceMarks { get; set; }
}

[Owned]
public sealed class MovementProgress
{
    public long LocationPathId { get; set; }
    public LocationPath LocationPath { get; set; } = null!;

    public long DistanceMarks { get; set; }
}
