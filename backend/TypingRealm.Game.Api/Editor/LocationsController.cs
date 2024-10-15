using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TypingRealm.Game.DataAccess;

namespace TypingRealm.Game.Api.Editor;

public sealed record CreateLocationDto(string Name, string Description, string Path);
public sealed record UpdateLocationDto(string Name, string Description, string Path);
public sealed record CreatePath(string Name, string Description, string ToLocationId, long DistanceMarks);
public sealed record UpdatePath(string ToLocationId, long DistanceMarks);

[Authorize] // TODO: Only allow ADMINS to manage the editor.
[Route("api/editor/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly GameDbContext _dbContext;

    public LocationsController(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async ValueTask<IEnumerable<Location>> GetAllLocations()
    {
        return await _dbContext.Location
            .ToListAsync();
    }

    [HttpPost]
    public async ValueTask<Location> CreateLocation(CreateLocationDto dto)
    {
        var location = new Location
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Description = dto.Description,
            Path = dto.Path
        };

        await _dbContext.Location.AddAsync(location);
        await _dbContext.SaveChangesAsync();

        // TODO: Return CreatedAtAction.
        return location;
    }

    [HttpPut("{locationId}")]
    public async ValueTask<ActionResult> UpdateLocation(string locationId, UpdateLocationDto dto)
    {
        var location = await _dbContext.Location.FindAsync(locationId);
        if (location is null)
            return NotFound();

        // TODO: Move this logic to Location DAO class.
        location.Name = dto.Name;
        location.Description = dto.Description;
        location.Path = dto.Path;

        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{locationId}/assets/{assetId}")]
    public async ValueTask<ActionResult<Asset>> AddAssetToLocation(string locationId, string assetId)
    {
        var location = await _dbContext.Location.FindAsync(locationId);
        if (location is null)
            return NotFound();

        var asset = await _dbContext.Asset.FindAsync(assetId);
        if (asset is null)
            return NotFound();

        location.Assets.Add(asset);
        await _dbContext.SaveChangesAsync();

        return asset;
    }

    [HttpDelete]
    public async ValueTask<ActionResult> DeleteAssetFromLocation(string locationId, string assetId)
    {
        var location = await _dbContext.Location.FindAsync(locationId);
        if (location is null)
            return NotFound();

        var asset = await _dbContext.Asset.FindAsync(assetId);
        if (asset is null)
            return NotFound();

        location.Assets.Remove(asset);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    [Route("{locationId}/paths")]
    public async ValueTask<ActionResult<LocationPath>> CreatePath(string locationId, CreatePath createPath)
    {
        var location = await _dbContext.Location.FindAsync(locationId);
        if (location is null)
            return NotFound();

        var path = new LocationPath
        {
            Name = createPath.Name,
            Description = createPath.Description,
            FromLocation = location,
            ToLocationId = createPath.ToLocationId,
            DistanceMarks = createPath.DistanceMarks
        };

        location.Paths.Add(path);
        await _dbContext.SaveChangesAsync();
        return path;
    }

    [HttpPut]
    [Route("{locationId}/paths/{pathId}")]
    public async ValueTask<ActionResult<LocationPath>> UpdatePath(
        string locationId, long pathId, UpdatePath updatePath)
    {
        var location = await _dbContext.Location.FindAsync(locationId);
        if (location is null)
            return NotFound();

        var existingPath = location.Paths.FirstOrDefault(p => p.Id == pathId);
        if (existingPath is null)
            return NotFound();

        existingPath.ToLocationId = updatePath.ToLocationId;
        existingPath.DistanceMarks = updatePath.DistanceMarks;
        await _dbContext.SaveChangesAsync();
        return existingPath;
    }
}

[Authorize]
[Route("api/editor/assets")]
public sealed class AssetsController : ControllerBase
{
    private readonly GameDbContext _dbContext;

    public AssetsController(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async ValueTask<Asset> CreateAsset(Asset asset)
    {
        asset.Id = Guid.NewGuid().ToString();

        await _dbContext.Asset.AddAsync(asset);
        await _dbContext.SaveChangesAsync();

        return asset;
    }
}
