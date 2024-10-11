using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TypingRealm.Game.DataAccess;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public required DbSet<Character> Character { get; init; }
}

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

public static class RegistrationExtensions
{
    public static IServiceCollection AddGameDataAccess(
        this IServiceCollection services, string connectionString)
    {
        return services.AddDbContextPool<GameDbContext>(options =>
        {
            options.UseSnakeCaseNamingConvention();
            options.UseNpgsql(connectionString);
        }).AddTransient<IStartupFilter, MigrateDatabaseStartupFilter>();
    }
}

internal sealed class MigrateDatabaseStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return async builder =>
        {
            await using var scope = builder.ApplicationServices.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            await dbContext.Database.MigrateAsync();
        };
    }
}
