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
        // 1) Ensure DB exists (Testcontainers)
        await ctx.Database.EnsureCreatedAsync();

        // 2) CLEAR ALL DATA (test/dev only)
        ctx.Boards.RemoveRange(ctx.Boards);
        ctx.Transactions.RemoveRange(ctx.Transactions);
        ctx.Players.RemoveRange(ctx.Players);
        ctx.Games.RemoveRange(ctx.Games);
        ctx.Users.RemoveRange(ctx.Users);

        await ctx.SaveChangesAsync();

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // ------------------------
        // TEST USER
        // ------------------------
        var salt = Guid.NewGuid().ToString();
        var testPassword = "Password123";

        var testHashBytes = SHA512.HashData(
            Encoding.UTF8.GetBytes(testPassword + salt));

        var testHash = BitConverter
            .ToString(testHashBytes)
            .Replace("-", "")
            .ToLower();

        ctx.Users.Add(new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@user.dk",
            Salt = salt,
            Passwordhash = testHash,
            Role = "User",
            Createdat = now
        });

        // ------------------------
        // FIXED ADMIN FOR DEV
        // ------------------------
        var adminId = "cbeaed9a-1466-4763-a3c9-3b10a26cf081";
        var adminEmail = "a@dp.dk";
        var adminSalt = "static-salt-admin";

        // Hash already computed correctly for Password123 + static-salt-admin
        var adminHash =
            "1b0b7aaebe570e09c7ee7d5c58604a373ec8dc3404b430e40df1f28c14d84a4294a0ff9e1a95491d806968f5c630d4ad723bb503422a9bc097b97b845eb9ca9d";

        ctx.Users.Add(new User
        {
            Id = adminId,
            Email = adminEmail,
            Salt = adminSalt,
            Passwordhash = adminHash,
            Role = "Admin",
            Createdat = now
        });

        // ------------------------
        // MINIMAL GAME
        // ------------------------
        ctx.Games.Add(new Game
        {
            Id = Guid.NewGuid().ToString(),
            Weeknumber = 45,
            Year = 2025,
            Isactive = true,
            Createdat = now
        });

        // ------------------------
        // SAVE
        // ------------------------
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
    }
}
