using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Etc;

/// <summary>
/// Shared seed logic used by both Dev and Test seeders.
/// This class does NOT clear the database and is designed to be idempotent.
/// </summary>
public static class SeedData
{
    public static async Task SeedCoreAsync(
        MyDbContext ctx,
        TimeProvider timeProvider,
        IPasswordHasher<User> passwordHasher)
    {
        // Always use UTC time for consistent test and dev data
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // ============================
        // USERS
        // ============================

        // Admin user (a@dp.dk, role Admin, password Password123)
        var adminEmail = "a@dp.dk";
        var admin = await ctx.Users
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (admin == null)
        {
            admin = new User
            {
                // Keep fixed Id for consistency with existing data and tests
                Id        = "cbeaed9a-1466-4763-a3c9-3b10a26cf081",
                Email     = adminEmail,
                Role      = "Admin",
                Createdat = now,
                Salt      = string.Empty
            };
            admin.Passwordhash = passwordHasher.HashPassword(admin, "Password123");

            ctx.Users.Add(admin);
            await ctx.SaveChangesAsync();
        }

        // Normal user that will be mapped to a Player (player@dp.dk)
        var playerEmail = "player@dp.dk";
        var playerUser = await ctx.Users
            .FirstOrDefaultAsync(u => u.Email == playerEmail);

        if (playerUser == null)
        {
            playerUser = new User
            {
                Id        = Guid.NewGuid().ToString(),
                Email     = playerEmail,
                Role      = "User",
                Createdat = now,
                Salt      = string.Empty
            };
            playerUser.Passwordhash = passwordHasher.HashPassword(playerUser, "Password123");

            ctx.Users.Add(playerUser);
            await ctx.SaveChangesAsync();
        }

        // Extra test user (not mapped to any Player)
        var testUserEmail = "test@user.dk";
        var testUser = await ctx.Users
            .FirstOrDefaultAsync(u => u.Email == testUserEmail);

        if (testUser == null)
        {
            testUser = new User
            {
                Id        = Guid.NewGuid().ToString(),
                Email     = testUserEmail,
                Role      = "User",
                Createdat = now,
                Salt      = string.Empty
            };
            testUser.Passwordhash = passwordHasher.HashPassword(testUser, "Password123");

            ctx.Users.Add(testUser);
            await ctx.SaveChangesAsync();
        }

        // ============================
        // PLAYER
        // ============================

        // Player is linked to user by Email (your design)
        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Email == playerEmail);

        if (player == null)
        {
            player = new Player
            {
                Id          = Guid.NewGuid().ToString(),
                Fullname    = "Test Player",
                Email       = playerEmail, // must match User.Email
                Phone       = "12345678",
                Isactive    = true,
                Activatedat = now,
                Createdat   = now,
                Deletedat   = null
            };
            ctx.Players.Add(player);
            await ctx.SaveChangesAsync();
        }

        // ============================
        // GAMES (one past game + one active game)
        // ============================

        Game pastGame;
        Game activeGame;

        if (!await ctx.Games.AnyAsync())
        {
            // When there are no games yet, we create:
            // - one past game (previous ISO week)
            // - one active game (future ISO week)
            //
            // Important:
            // We intentionally pick an active game in the future (now + 14 days),
            // so that the "Saturday 17:00 Danish time" cutoff in BoardService
            // will NOT block buying boards in local development.

            var referenceDate = now.AddDays(14);      // ~ two weeks in the future
            var pastDate      = referenceDate.AddDays(-7); // previous ISO week

            var activeYear = System.Globalization.ISOWeek.GetYear(referenceDate);
            var activeWeek = System.Globalization.ISOWeek.GetWeekOfYear(referenceDate);

            var pastYear = System.Globalization.ISOWeek.GetYear(pastDate);
            var pastWeek = System.Globalization.ISOWeek.GetWeekOfYear(pastDate);

            pastGame = new Game
            {
                Id             = Guid.NewGuid().ToString(),
                Weeknumber     = pastWeek,
                Year           = pastYear,
                Isactive       = false,
                Createdat      = pastDate.AddDays(-7),
                Closedat       = pastDate,
                Deletedat      = null,
                Winningnumbers = new List<int> { 1, 5, 9 }
            };
            ctx.Games.Add(pastGame);

            activeGame = new Game
            {
                Id             = Guid.NewGuid().ToString(),
                Weeknumber     = activeWeek,
                Year           = activeYear,
                Isactive       = true,
                Createdat      = referenceDate,
                Closedat       = null,
                Deletedat      = null,
                Winningnumbers = null
            };
            ctx.Games.Add(activeGame);

            await ctx.SaveChangesAsync();
        }
        else
        {
            // Re-use existing games if they already exist
            activeGame = await ctx.Games
                .Where(g => g.Isactive)
                .OrderByDescending(g => g.Createdat)
                .FirstAsync();

            pastGame = await ctx.Games
                .Where(g => !g.Isactive && g.Winningnumbers != null)
                .OrderByDescending(g => g.Closedat)
                .FirstOrDefaultAsync()
                ?? await ctx.Games
                    .Where(g => !g.Isactive)
                    .OrderByDescending(g => g.Createdat)
                    .FirstAsync();
        }
        // ============================
        // DEBUG / DEMO: cutoff rule (DEV only)
        // ============================
        //  IMPORTANT:
        // This block is ONLY for manual testing in Development.
        // If you want to simulate that the deadline (Saturday 17:00 Danish time)
        // is already over for the ACTIVE game, you can temporarily uncomment this.
        //
        // When enabled, it forces the active game to be in the past
        // (for example ISO week 1 of year 2020), so the BoardService
        // will always throw the "You can no longer join this week's game..."
        // validation error when trying to buy a board for the active game.
        //
        // Remember to comment it back before committing / deploying.
        //and also commit part in DevSeeder.cs
        //cd server
        // docker compose down -v
        // docker compose up -d
        //dotnet run 
        // 
        // #if DEBUG
        // activeGame.Year = 2020;
        // activeGame.Weeknumber = 1;
        // ctx.Games.Update(activeGame);
        // await ctx.SaveChangesAsync();
        // #endif


        // ============================
        // FUTURE GAMES (state-less API tip)
        // ============================

        // Pre-generate "inactive" games for the next ~20 years.
        // Later the API will just close the current game and activate the next one.
        var existingKeys = await ctx.Games
            .Select(g => new { g.Year, g.Weeknumber })
            .ToListAsync();

        var existingSet = new HashSet<(int year, int week)>(
            existingKeys.Select(x => (x.Year, x.Weeknumber)));

        var yearCursor    = activeGame.Year;
        var weekCursor    = activeGame.Weeknumber;
        var createdCursor = activeGame.Createdat ?? now;

        const int weeksToCreate = 20 * 52; // about 20 years

        for (var i = 0; i < weeksToCreate; i++)
        {
            // move to the next week
            if (weekCursor < 52)
            {
                weekCursor++;
            }
            else
            {
                weekCursor = 1;
                yearCursor++;
            }

            createdCursor = createdCursor.AddDays(7);

            if (existingSet.Contains((yearCursor, weekCursor)))
            {
                continue; // game already exists for this week/year
            }

            var futureGame = new Game
            {
                Id             = Guid.NewGuid().ToString(),
                Weeknumber     = weekCursor,
                Year           = yearCursor,
                Isactive       = false,
                Createdat      = createdCursor,
                Closedat       = null,
                Deletedat      = null,
                Winningnumbers = null
            };

            ctx.Games.Add(futureGame);
            existingSet.Add((yearCursor, weekCursor));
        }

        await ctx.SaveChangesAsync();

        // ============================
        // TRANSACTIONS (MobilePay)
        // ============================

        // Approved transaction so player has some balance
        const string approvedMp = "MP-0001";
        if (!await ctx.Transactions.AnyAsync(t => t.Mobilepaynumber == approvedMp))
        {
            var approvedTransaction = new Transaction
            {
                Id              = Guid.NewGuid().ToString(),
                Playerid        = player.Id,
                Mobilepaynumber = approvedMp,
                Amount          = 200, // DKK
                Status          = "Approved",
                Createdat       = now.AddMinutes(-30),
                Approvedat      = now.AddMinutes(-20),
                Deletedat       = null
            };
            ctx.Transactions.Add(approvedTransaction);
        }

        // Pending transaction to show pending list on admin Payments tab
        const string pendingMp = "MP-0002";
        if (!await ctx.Transactions.AnyAsync(t => t.Mobilepaynumber == pendingMp))
        {
            var pendingTransaction = new Transaction
            {
                Id              = Guid.NewGuid().ToString(),
                Playerid        = player.Id,
                Mobilepaynumber = pendingMp,
                Amount          = 100,
                Status          = "Pending",
                Createdat       = now.AddMinutes(-10),
                Approvedat      = null,
                Deletedat       = null
            };
            ctx.Transactions.Add(pendingTransaction);
        }

        await ctx.SaveChangesAsync();

        // ============================
        // BOARDS (guesses)
        // ============================

        // Winning board for the past game (must contain all winning numbers)
        var hasPastWinningBoard = await ctx.Boards
            .AnyAsync(b => b.Gameid == pastGame.Id && b.Iswinning);

        if (!hasPastWinningBoard)
        {
            var winningNumbers = pastGame.Winningnumbers ?? new List<int> { 1, 5, 9 };

            var pastWinningBoard = new Board
            {
                Id           = Guid.NewGuid().ToString(),
                Playerid     = player.Id,
                Gameid       = pastGame.Id,
                Numbers      = new List<int>(winningNumbers) { 10, 11 }, // includes all winning numbers
                Price        = 20,
                Iswinning    = true,
                Repeatweeks  = 0,
                Repeatactive = false,
                Createdat    = now.AddDays(-10),
                Deletedat    = null
            };
            ctx.Boards.Add(pastWinningBoard);
        }

        // Non-winning boards for the active game
        var hasActiveBoards = await ctx.Boards
            .AnyAsync(b => b.Gameid == activeGame.Id && b.Playerid == player.Id);

        if (!hasActiveBoards)
        {
            var activeBoard1 = new Board
            {
                Id           = Guid.NewGuid().ToString(),
                Playerid     = player.Id,
                Gameid       = activeGame.Id,
                Numbers      = new List<int> { 1, 2, 3, 4, 5 }, // 5 numbers
                Price        = 20,
                Iswinning    = false,
                Repeatweeks  = 0,
                Repeatactive = false,
                Createdat    = now,
                Deletedat    = null
            };

            var activeBoard2 = new Board
            {
                Id           = Guid.NewGuid().ToString(),
                Playerid     = player.Id,
                Gameid       = activeGame.Id,
                Numbers      = new List<int> { 3, 6, 9, 12, 14, 16 }, // 6 numbers
                Price        = 40,
                Iswinning    = false,
                Repeatweeks  = 1,
                Repeatactive = true,
                Createdat    = now,
                Deletedat    = null
            };

            ctx.Boards.AddRange(activeBoard1, activeBoard2);
        }

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
    }
}
