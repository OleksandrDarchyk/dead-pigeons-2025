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
        // Dev/Test only: ensure database exists
        await ctx.Database.EnsureCreatedAsync();

        // WARNING: Dev/Test only!
        // This wipes all data so we always start from a known clean state.
        ctx.Boards.RemoveRange(ctx.Boards);
        ctx.Transactions.RemoveRange(ctx.Transactions);
        ctx.Players.RemoveRange(ctx.Players);
        ctx.Games.RemoveRange(ctx.Games);
        ctx.Users.RemoveRange(ctx.Users);

        await ctx.SaveChangesAsync();

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // ------------------------
        // AUTH USERS
        // ------------------------

        // Fixed admin user (for login to admin panel)
        var adminId = "cbeaed9a-1466-4763-a3c9-3b10a26cf081";
        var admin = new User
        {
            Id = adminId,
            Email = "a@dp.dk",
            Role = "Admin",
            Createdat = now,
            Salt = string.Empty // salt column is kept but not used with Argon2
        };
        admin.Passwordhash = passwordHasher.HashPassword(admin, "Password123");
        ctx.Users.Add(admin);

        // Normal user that will be mapped to a Player (used for /player area)
        var playerUserId = Guid.NewGuid().ToString();
        var playerUser = new User
        {
            Id = playerUserId,
            Email = "player@dp.dk",
            Role = "User",
            Createdat = now,
            Salt = string.Empty
        };
        playerUser.Passwordhash = passwordHasher.HashPassword(playerUser, "Password123");
        ctx.Users.Add(playerUser);

        // Optional extra test user (not mapped to any Player)
        var testUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@user.dk",
            Role = "User",
            Createdat = now,
            Salt = string.Empty
        };
        testUser.Passwordhash = passwordHasher.HashPassword(testUser, "Password123");
        ctx.Users.Add(testUser);

        // ------------------------
        // PLAYERS
        // ------------------------

        // This Player is linked to player@dp.dk via email.
        // Resource-based methods (GetMyBoards, GetMyBalance, etc.) will use this.
        var playerId = Guid.NewGuid().ToString();
        var player = new Player
        {
            Id = playerId,
            Fullname = "Test Player",
            Email = "player@dp.dk",       // must match User.Email for resource-based endpoints
            Phone = "12345678",
            Isactive = true,
            Activatedat = now,
            Createdat = now,
            Deletedat = null
        };
        ctx.Players.Add(player);

        // ------------------------
        // GAMES (one past game + one active game)
        // ------------------------

        var pastGameId = Guid.NewGuid().ToString();
        var pastGame = new Game
        {
            Id = pastGameId,
            Weeknumber = 47,
            Year = 2025,
            Isactive = false,
            Createdat = now.AddDays(-14),
            Closedat = now.AddDays(-7),
            Deletedat = null,
            Winningnumbers = new List<int> { 1, 5, 9 } // example winning numbers
        };
        ctx.Games.Add(pastGame);

        var activeGameId = Guid.NewGuid().ToString();
        var activeGame = new Game
        {
            Id = activeGameId,
            Weeknumber = 48,
            Year = 2025,
            Isactive = true,
            Createdat = now,
            Closedat = null,
            Deletedat = null
        };
        ctx.Games.Add(activeGame);

        // ------------------------
        // FUTURE GAMES (pre-seeded rounds)
        // ------------------------

        // We pre-generate many future weekly games so the API can always activate the next one
        var futureWeek = activeGame.Weeknumber;
        var futureYear = activeGame.Year;
        var futureCreatedAt = activeGame.Createdat ?? now;

        // How many future games to pre-generate (dev/test only)
        const int futureGamesToCreate = 200;

        for (var i = 0; i < futureGamesToCreate; i++)
        {
            // Simple week/year increment: 1..52 then wrap to next year
            if (futureWeek < 52)
            {
                futureWeek++;
            }
            else
            {
                futureWeek = 1;
                futureYear++;
            }

            futureCreatedAt = futureCreatedAt.AddDays(7);

            var futureGame = new Game
            {
                Id = Guid.NewGuid().ToString(),
                Weeknumber = futureWeek,
                Year = futureYear,
                Isactive = false,
                Createdat = futureCreatedAt,
                Closedat = null,
                Deletedat = null,
                Winningnumbers = null
            };

            ctx.Games.Add(futureGame);
        }

        // ------------------------
        // TRANSACTIONS (MobilePay)
        // ------------------------

        // Approved transaction so player has some balance
        var approvedTransaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = playerId,
            Mobilepaynumber = "MP-0001",
            Amount = 200, // DKK
            Status = "Approved",
            Createdat = now.AddMinutes(-30),
            Approvedat = now.AddMinutes(-20),
            Deletedat = null
        };
        ctx.Transactions.Add(approvedTransaction);

        // Pending transaction to show pending list on admin Payments tab
        var pendingTransaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = playerId,
            Mobilepaynumber = "MP-0002",
            Amount = 100,
            Status = "Pending",
            Createdat = now.AddMinutes(-10),
            Approvedat = null,
            Deletedat = null
        };
        ctx.Transactions.Add(pendingTransaction);

        // ------------------------
        // BOARDS (guesses)
        // ------------------------

        // For the active game (week 48)
        var activeBoard1 = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = playerId,
            Gameid = activeGameId,
            Numbers = new List<int> { 1, 2, 3, 4, 5 }, // 5 numbers
            Price = 20,                                // 5 numbers -> 20 DKK
            Iswinning = false,
            Repeatweeks = 0,
            Repeatactive = false,
            Createdat = now,
            Deletedat = null
        };
        ctx.Boards.Add(activeBoard1);

        var activeBoard2 = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = playerId,
            Gameid = activeGameId,
            Numbers = new List<int> { 3, 6, 9, 12, 14, 16 }, // 6 numbers
            Price = 40,                                     // 6 numbers -> 40 DKK
            Iswinning = false,
            Repeatweeks = 1,
            Repeatactive = true,
            Createdat = now,
            Deletedat = null
        };
        ctx.Boards.Add(activeBoard2);

        // For the past game (week 47) – this one is a winning board
        var pastWinningBoard = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = playerId,
            Gameid = pastGameId,
            Numbers = new List<int> { 1, 5, 9, 10, 11 }, // contains all winning numbers 1,5,9
            Price = 20,
            Iswinning = true,                            // consistent with game's winningNumbers
            Repeatweeks = 0,
            Repeatactive = false,
            Createdat = now.AddDays(-10),
            Deletedat = null
        };
        ctx.Boards.Add(pastWinningBoard);

        // ------------------------
        // SAVE ALL SEEDED DATA
        // ------------------------

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
    }
}
