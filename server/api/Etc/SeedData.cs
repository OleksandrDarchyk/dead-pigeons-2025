
using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Etc;

// Shared seed logic used by both Dev and Test seeders.
// This class does NOT clear the database and is designed to be idempotent.

public static class SeedData
{
    public static async Task SeedCoreAsync(
        MyDbContext ctx,
        TimeProvider timeProvider,
        IPasswordHasher<User> passwordHasher)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var adminEmail = "a@dp.dk";
        var admin = await ctx.Users
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (admin == null)
        {
            admin = new User
            {
                Id        = "cbeaed9a-1466-4763-a3c9-3b10a26cf081",
                Email     = adminEmail,
                Role      = "Admin",
                Createdat = now,
            };
            admin.Passwordhash = passwordHasher.HashPassword(admin, "Password123");

            ctx.Users.Add(admin);
            await ctx.SaveChangesAsync();
        }

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
            };
            playerUser.Passwordhash = passwordHasher.HashPassword(playerUser, "Password123");

            ctx.Users.Add(playerUser);
            await ctx.SaveChangesAsync();
        }

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
            };
            testUser.Passwordhash = passwordHasher.HashPassword(testUser, "Password123");

            ctx.Users.Add(testUser);
            await ctx.SaveChangesAsync();
        }
        
        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Email == playerEmail);

        if (player == null)
        {
            player = new Player
            {
                Id          = Guid.NewGuid().ToString(),
                Fullname    = "Test Player",
                Email       = playerEmail, 
                Phone       = "12345678",
                Isactive    = true,
                Activatedat = now,
                Createdat   = now,
                Deletedat   = null
            };
            ctx.Players.Add(player);
            await ctx.SaveChangesAsync();
        }
        

        Game pastGame;
        Game activeGame;

        if (!await ctx.Games.AnyAsync())
        {
            
            var referenceDate = now.AddDays(14);     
            var pastDate      = referenceDate.AddDays(-7); 

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
      
        var existingKeys = await ctx.Games
            .Select(g => new { g.Year, g.Weeknumber })
            .ToListAsync();

        var existingSet = new HashSet<(int year, int week)>(
            existingKeys.Select(x => (x.Year, x.Weeknumber)));

        var yearCursor    = activeGame.Year;
        var weekCursor    = activeGame.Weeknumber;
        var createdCursor = activeGame.Createdat ?? now;

        const int weeksToCreate = 20 * 52; 

        for (var i = 0; i < weeksToCreate; i++)
        {
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
                continue; 
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
                Numbers      = new List<int>(winningNumbers) { 10, 11 },
                Price        = 20,
                Iswinning    = true,
                Repeatweeks  = 0,
                Repeatactive = false,
                Createdat    = now.AddDays(-10),
                Deletedat    = null
            };
            ctx.Boards.Add(pastWinningBoard);
        }

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
