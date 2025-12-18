using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;

namespace api.Etc;

public class TestSeeder(
    MyDbContext ctx,
    TimeProvider timeProvider,
    IPasswordHasher<User> passwordHasher) : ISeeder
{
    public async Task Seed()
    {
        await ctx.Database.EnsureCreatedAsync();
        
        ctx.Boards.RemoveRange(ctx.Boards);
        ctx.Transactions.RemoveRange(ctx.Transactions);
        ctx.Players.RemoveRange(ctx.Players);
        ctx.Games.RemoveRange(ctx.Games);
        ctx.Users.RemoveRange(ctx.Users);

        await ctx.SaveChangesAsync();

        await SeedData.SeedCoreAsync(ctx, timeProvider, passwordHasher);
    }
}