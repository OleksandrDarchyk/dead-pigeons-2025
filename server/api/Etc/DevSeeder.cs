// api/Etc/DevSeeder.cs
using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Etc;

/// <summary>
/// Seeder used in Development environment.
/// - Does NOT clear the database.
/// - Seeds core demo data only once when the DB is empty (based on Users).
/// - Ensures games exist (Tip 1) via GameSeeder in an idempotent way.
/// </summary>
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