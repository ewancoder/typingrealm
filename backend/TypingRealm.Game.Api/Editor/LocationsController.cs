using Microsoft.AspNetCore.Mvc;
using TypingRealm.Game.DataAccess;

namespace TypingRealm.Game.Api.Editor;

public sealed record CreateLocationDto(string Name, string Description);
public sealed record UpdateLocationDto(string Name, string Description);
public sealed record CreatePath(string ToLocationId, long DistanceMarks);
public sealed record UpdatePath(string ToLocationId, long DistanceMarks);

[Route("editor/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly GameDbContext _dbContext;

    public LocationsController(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async ValueTask<Location> CreateLocation(CreateLocationDto dto)
    {
        var location = new Location
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Description = dto.Description
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
