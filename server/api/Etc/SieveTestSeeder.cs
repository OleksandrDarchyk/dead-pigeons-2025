using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;

namespace api.Etc;

/// <summary>
/// Seeder used ONLY for tests.
/// It always starts from a clean database and then calls the shared SieveSeedData.
/// </summary>
public class SieveTestSeeder(
    MyDbContext ctx,
    TimeProvider timeProvider,
    IPasswordHasher<User> passwordHasher) : ISeeder
{
    public async Task Seed()
    {
        // Dev/Test only: ensure database exists
        await ctx.Database.EnsureCreatedAsync();

        // WARNING: Tests only!
        // This wipes all data so we always start from a known clean state.
        ctx.Boards.RemoveRange(ctx.Boards);
        ctx.Transactions.RemoveRange(ctx.Transactions);
        ctx.Players.RemoveRange(ctx.Players);
        ctx.Games.RemoveRange(ctx.Games);
        ctx.Users.RemoveRange(ctx.Users);

        await ctx.SaveChangesAsync();

        // Re-use shared seed logic so Dev and Tests have the same basic data.
        await SieveSeedData.SeedCoreAsync(ctx, timeProvider, passwordHasher);
    }
}