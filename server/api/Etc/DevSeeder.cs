using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Etc;

/// <summary>
/// Seeder used in Development environment.
/// It does NOT clear the database. It only seeds once when the DB is empty.
/// </summary>
public class DevSeeder(
    MyDbContext ctx,
    TimeProvider timeProvider,
    IPasswordHasher<User> passwordHasher) : ISeeder
{
    public async Task Seed()
    {
        // Ensure the database is created (Dev only, not for production migrations).
        await ctx.Database.EnsureCreatedAsync();

        // If there is at least one user, we assume the database is already initialized.
        var hasAnyUsers = await ctx.Users.AnyAsync();
        if (hasAnyUsers)
        {
            // Dev seeder should be safe: do not touch or modify existing data.
            return;
        }

        // Seed the shared core data (users, players, games, transactions, boards).
        // This uses the same core logic as the Test seeder (TestSeeder),
        // so Dev and Tests work with the same data model and examples.
        await SeedData.SeedCoreAsync(ctx, timeProvider, passwordHasher);
    }
}