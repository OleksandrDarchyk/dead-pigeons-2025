using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Etc;

/// Seeder used in Development environment.
/// - Seeds  data only once when the DB is empty 
public class DevSeeder(
    MyDbContext ctx,
    TimeProvider timeProvider,
    IPasswordHasher<User> passwordHasher,
    GameSeeder gameSeeder) : ISeeder
{
    public async Task Seed()
    {
        await ctx.Database.EnsureCreatedAsync();

        var hasAnyUsers = await ctx.Users.AnyAsync();
        if (!hasAnyUsers)
        {
            await SeedData.SeedCoreAsync(ctx, timeProvider, passwordHasher);
        }

        await gameSeeder.SeedGamesIfMissingAsync();
    }
}