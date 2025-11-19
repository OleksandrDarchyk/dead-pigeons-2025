using System.Security.Cryptography;
using System.Text;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Etc;

public class SieveTestSeeder(MyDbContext ctx, TimeProvider timeProvider) : ISeeder
{
    public async Task Seed()
    {
        // 1) Create all tables in the current database (Testcontainers)
        await ctx.Database.EnsureCreatedAsync();

        // 2) Just in case, clear data (so each run starts from scratch)
        ctx.Boards.RemoveRange(ctx.Boards);
        ctx.Transactions.RemoveRange(ctx.Transactions);
        ctx.Players.RemoveRange(ctx.Players);
        ctx.Games.RemoveRange(ctx.Games);
        ctx.Users.RemoveRange(ctx.Users);
        await ctx.SaveChangesAsync();

        // 3) Create a test user for login
        //    Email: test@user.dk
        //    Password: Password123 (hashed the same way as in AuthService.Register)
        var salt = Guid.NewGuid().ToString();
        var password = "Password123";

        var hashBytes = SHA512.HashData(
            Encoding.UTF8.GetBytes(password + salt));

        var passwordHash = hashBytes.Aggregate(
            "",
            (current, b) => current + b.ToString("x2"));

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@user.dk",
            Salt = salt,
            Passwordhash = passwordHash,
            Role = "User",
            Createdat = now
        };

        ctx.Users.Add(user);

        // 4) (minimal) create one active Game,
        //    so the frontend does not crash when it asks for an active game
        var game = new Game
        {
            Id = Guid.NewGuid().ToString(),
            Weeknumber = 45,
            Year = 2025,
            Winningnumbers = null,
            Isactive = true,
            Createdat = now,
            Closedat = null,
            Deletedat = null
        };

        ctx.Games.Add(game);

        // 5) Save changes
        await ctx.SaveChangesAsync();

        // 6) Clear the change tracker (so EF does not keep a lot of entities in memory)
        ctx.ChangeTracker.Clear();
    }
}
