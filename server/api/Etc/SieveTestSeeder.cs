using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Etc;

public class SieveTestSeeder(
    MyDbContext ctx,
    TimeProvider timeProvider,
    IPasswordHasher<User> passwordHasher) : ISeeder
{
    public async Task Seed()
    {
        // Ensure database exists (dev/test only)
        await ctx.Database.EnsureCreatedAsync();

        // Clear all data (dev/test only)
        ctx.Boards.RemoveRange(ctx.Boards);
        ctx.Transactions.RemoveRange(ctx.Transactions);
        ctx.Players.RemoveRange(ctx.Players);
        ctx.Games.RemoveRange(ctx.Games);
        ctx.Users.RemoveRange(ctx.Users);

        await ctx.SaveChangesAsync();

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // ------------------------
        // TEST USER (argon2id)
        // ------------------------
        var testUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@user.dk",
            Role = "User",
            Createdat = now,
            Salt = "" // salt is embedded in Passwordhash now
        };

        testUser.Passwordhash = passwordHasher.HashPassword(testUser, "Password123");
        ctx.Users.Add(testUser);

        // ------------------------
        // FIXED ADMIN FOR DEV (argon2id)
        // ------------------------
        var admin = new User
        {
            Id = "cbeaed9a-1466-4763-a3c9-3b10a26cf081",
            Email = "a@dp.dk",
            Role = "Admin",
            Createdat = now,
            Salt = "" // we keep column but do not use it
        };

        admin.Passwordhash = passwordHasher.HashPassword(admin, "Password123");
        ctx.Users.Add(admin);

        // ------------------------
        // MINIMAL ACTIVE GAME
        // ------------------------
        ctx.Games.Add(new Game
        {
            Id = Guid.NewGuid().ToString(),
            Weeknumber = 45,
            Year = 2025,
            Isactive = true,
            Createdat = now
        });

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
    }
}
