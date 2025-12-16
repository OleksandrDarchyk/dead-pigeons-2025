// server/tests/BoardServiceTests.cs
using System.Globalization;
using System.Security.Claims;
using api.Models.Requests;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using ValidationException = Bogus.ValidationException;
using DataAnnotationValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace tests;

/// <summary>
/// Service-level tests for BoardService.
/// Covers:
/// - create board validation + balance rules
/// - active/inactive player + active/inactive game rules
/// - claim-based "current user" resolution
/// - Saturday 17:00 DK cutoff rule
/// - read methods ordering + soft delete filtering
/// </summary>
public class BoardServiceTests(
    IBoardService boardService,
    MyDbContext ctx,
    TestTransactionScope transaction,
    TimeProvider timeProvider,
    ITestOutputHelper outputHelper) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await transaction.BeginTransactionAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        // IMPORTANT: guarantee rollback per test
        transaction.Dispose();
        return ValueTask.CompletedTask;
    }

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------

    private async Task<Game> CreateUniqueGame(bool isActive = true)
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Push into far future to avoid collisions with seeded games.
        var year = now.Year + 20;

        var existingWeeks = await ctx.Games
            .Where(g => g.Year == year)
            .Select(g => g.Weeknumber)
            .ToListAsync(ct);

        var weekNumber = Enumerable.Range(1, 52)
            .First(w => !existingWeeks.Contains(w));

        var game = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = year,
            Weeknumber = weekNumber,
            Isactive   = isActive,
            Createdat  = now,
            Closedat   = null,
            Deletedat  = null
        };

        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);
        return game;
    }

    private async Task<Player> CreateUniquePlayer(
        string emailPrefix,
        int balance = 0,
        bool isActive = true)
    {
        var ct       = TestContext.Current.CancellationToken;
        var now      = timeProvider.GetUtcNow().UtcDateTime;
        var uniqueId = Guid.NewGuid().ToString();

        var player = new Player
        {
            Id          = uniqueId,
            Fullname    = $"Test Player {uniqueId[..8]}",
            Email       = $"{emailPrefix}-{uniqueId}@test.local",
            Phone       = "12345678",
            Isactive    = isActive,
            Activatedat = isActive ? now : null,
            Createdat   = now,
            Deletedat   = null
        };

        ctx.Players.Add(player);

        if (balance > 0)
        {
            var tx = new Transaction
            {
                Id              = Guid.NewGuid().ToString(),
                Playerid        = player.Id,
                Amount          = balance,
                Status          = "Approved",
                Mobilepaynumber = $"{now.Ticks}{Random.Shared.Next(1000, 9999)}",
                Createdat       = now,
                Approvedat      = now,
                Deletedat       = null
            };
            ctx.Transactions.Add(tx);
        }

        await ctx.SaveChangesAsync(ct);
        return player;
    }

    // ------------------------------------------------------------
    // CreateBoard
    // ------------------------------------------------------------

    [Fact]
    public async Task CreateBoard_Succeeds_When_PlayerHasEnoughBalance_AndValidNumbers()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = await CreateUniquePlayer("board-happy", balance: 100);
        var game   = await CreateUniqueGame(isActive: true);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 5, 1, 4, 2, 3 },
            RepeatWeeks = 0
        };

        var board = await boardService.CreateBoard(player.Id, dto);

        Assert.False(string.IsNullOrWhiteSpace(board.Id));
        Assert.Equal(player.Id, board.Playerid);
        Assert.Equal(game.Id, board.Gameid);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, board.Numbers.OrderBy(n => n));
        Assert.Equal(20, board.Price);

        var approvedAmount = await ctx.Transactions
            .Where(t => t.Playerid == player.Id && t.Deletedat == null && t.Status == "Approved")
            .SumAsync(t => (int?)t.Amount, ct) ?? 0;

        var spentOnBoards = await ctx.Boards
            .Where(b => b.Playerid == player.Id && b.Deletedat == null)
            .SumAsync(b => (int?)b.Price, ct) ?? 0;

        Assert.Equal(80, approvedAmount - spentOnBoards);

        outputHelper.WriteLine(
            $"[Board] Player {player.Id} bought board {board.Id}, price={board.Price}, balanceLeft={approvedAmount - spentOnBoards}"
        );
    }

    [Fact]
    public async Task CreateBoard_Throws_When_PlayerHasNotEnoughBalance()
    {
        var player = await CreateUniquePlayer("no-balance", balance: 0);
        var game   = await CreateUniqueGame(isActive: true);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 1
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto));
    }

    [Fact]
    public async Task CreateBoard_Throws_When_NumbersCountIsInvalid()
    {
        var player = await CreateUniquePlayer("count-invalid", balance: 1000);
        var game   = await CreateUniqueGame(isActive: true);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4 }, // 4 numbers
            RepeatWeeks = 0
        };

        // If DTO has MinLength/MaxLength -> DataAnnotations exception.
        await Assert.ThrowsAsync<DataAnnotationValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto));
    }

    [Fact]
    public async Task CreateBoard_Throws_When_NumbersAreNotDistinct()
    {
        var player = await CreateUniquePlayer("duplicate-numbers", balance: 1000);
        var game   = await CreateUniqueGame(isActive: true);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 1, 2, 3, 4 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto));
    }

    [Fact]
    public async Task CreateBoard_Throws_When_NumberOutOfRange()
    {
        var player = await CreateUniquePlayer("range-invalid", balance: 1000);
        var game   = await CreateUniqueGame(isActive: true);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 17 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto));
    }

    [Fact]
    public async Task CreateBoard_Throws_When_PlayerNotFound()
    {
        var game = await CreateUniqueGame(isActive: true);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(Guid.NewGuid().ToString(), dto));
    }

    [Fact]
    public async Task CreateBoard_Throws_When_PlayerIsInactive()
    {
        var player = await CreateUniquePlayer("inactive", balance: 1000, isActive: false);
        var game   = await CreateUniqueGame(isActive: true);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto));
    }

    [Fact]
    public async Task CreateBoard_Throws_When_GameNotFound()
    {
        var player = await CreateUniquePlayer("no-game", balance: 1000);

        var dto = new CreateBoardRequestDto
        {
            GameId      = Guid.NewGuid().ToString(),
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto));
    }

    [Fact]
    public async Task CreateBoard_Throws_When_GameIsInactive()
    {
        var player = await CreateUniquePlayer("inactive-game", balance: 1000);
        var game   = await CreateUniqueGame(isActive: false);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto));
    }

    // ------------------------------------------------------------
    // GetBoardsForGame / GetBoardsForPlayer
    // ------------------------------------------------------------

    [Fact]
    public async Task GetBoardsForGame_Returns_OnlyNonDeletedBoards_ForGivenGame_OrderedByCreatedAt()
    {
        var ct = TestContext.Current.CancellationToken;

        var player     = await CreateUniquePlayer("boards-game");
        var targetGame = await CreateUniqueGame(isActive: true);
        var otherGame  = await CreateUniqueGame(isActive: false);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var boardOld = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = targetGame.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 1,
            Repeatactive = false,
            Createdat    = now.AddMinutes(1),
            Deletedat    = null
        };

        var boardDeleted = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = targetGame.Id,
            Numbers      = new[] { 6, 7, 8, 9, 10 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 1,
            Repeatactive = false,
            Createdat    = now.AddMinutes(2),
            Deletedat    = now.AddMinutes(3)
        };

        var boardNew = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = targetGame.Id,
            Numbers      = new[] { 11, 12, 13, 14, 15 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 1,
            Repeatactive = false,
            Createdat    = now.AddMinutes(4),
            Deletedat    = null
        };

        var boardOtherGame = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = otherGame.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 1,
            Repeatactive = false,
            Createdat    = now.AddMinutes(5),
            Deletedat    = null
        };

        ctx.Boards.AddRange(boardOld, boardDeleted, boardNew, boardOtherGame);
        await ctx.SaveChangesAsync(ct);

        var result = await boardService.GetBoardsForGame(targetGame.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, b =>
        {
            Assert.Equal(targetGame.Id, b.Gameid);
            Assert.Null(b.Deletedat);
        });

        Assert.Equal(new[] { boardOld.Id, boardNew.Id }, result.Select(b => b.Id).ToArray());
    }

    [Fact]
    public async Task GetBoardsForPlayer_Returns_OnlyNonDeletedBoards_ForGivenPlayer_OrderedNewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;

        var player      = await CreateUniquePlayer("boards-player");
        var otherPlayer = await CreateUniquePlayer("other-player");
        var game        = await CreateUniqueGame(isActive: true);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var olderBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 0,
            Repeatactive = false,
            Createdat    = now.AddMinutes(-10),
            Deletedat    = null
        };

        var newerBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 6, 7, 8, 9, 10 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 0,
            Repeatactive = false,
            Createdat    = now.AddMinutes(-5),
            Deletedat    = null
        };

        var deletedBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 0,
            Repeatactive = false,
            Createdat    = now.AddMinutes(-3),
            Deletedat    = now
        };

        var otherPlayerBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = otherPlayer.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 0,
            Repeatactive = false,
            Createdat    = now.AddMinutes(-1),
            Deletedat    = null
        };

        ctx.Boards.AddRange(olderBoard, newerBoard, deletedBoard, otherPlayerBoard);
        await ctx.SaveChangesAsync(ct);

        var result = await boardService.GetBoardsForPlayer(player.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(player.Id, b.Playerid));
        Assert.Equal(new[] { newerBoard.Id, olderBoard.Id }, result.Select(b => b.Id).ToArray());
    }

    // ------------------------------------------------------------
    // Current user (claims)
    // ------------------------------------------------------------

    [Fact]
    public async Task CreateBoardForCurrentUser_ResolvesPlayerFromEmailClaim_AndCreatesBoard()
    {
        var player = await CreateUniquePlayer("claims-player", balance: 100);
        var game   = await CreateUniqueGame(isActive: true);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, player.Email) },
            authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        var board = await boardService.CreateBoardForCurrentUser(user, dto);

        Assert.Equal(player.Id, board.Playerid);
        Assert.Equal(game.Id, board.Gameid);
    }

    [Fact]
    public async Task CreateBoardForCurrentUser_Throws_When_EmailClaimMissing()
    {
        var identity = new ClaimsIdentity();
        var user     = new ClaimsPrincipal(identity);

        var dto = new CreateBoardRequestDto
        {
            GameId      = Guid.NewGuid().ToString(),
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoardForCurrentUser(user, dto));
    }

    [Fact]
    public async Task GetBoardsForCurrentUser_Returns_BoardsForResolvedPlayer()
    {
        var ct = TestContext.Current.CancellationToken;

        var player      = await CreateUniquePlayer("history-claims");
        var otherPlayer = await CreateUniquePlayer("other-claims");
        var game        = await CreateUniqueGame(isActive: true);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var boardForPlayer = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 0,
            Repeatactive = false,
            Createdat    = now.AddMinutes(-5),
            Deletedat    = null
        };

        var boardForOther = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = otherPlayer.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 6, 7, 8, 9, 10 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 0,
            Repeatactive = false,
            Createdat    = now.AddMinutes(-1),
            Deletedat    = null
        };

        ctx.Boards.AddRange(boardForPlayer, boardForOther);
        await ctx.SaveChangesAsync(ct);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, player.Email) },
            authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var result = await boardService.GetBoardsForCurrentUser(user);

        Assert.Single(result);
        Assert.Equal(player.Id, result[0].Playerid);
    }

    // ------------------------------------------------------------
    // Saturday 17:00 DK cutoff
    // ------------------------------------------------------------

    private static TimeZoneInfo GetDanishTimeZoneForTests()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen"); }
        catch (TimeZoneNotFoundException)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"); }
            catch { return TimeZoneInfo.Local; }
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    private static DateTime GetDanishSaturdayBeforeCutoffUtc(Game game)
    {
        var dkZone = GetDanishTimeZoneForTests();

        var saturdayLocal = ISOWeek
            .ToDateTime(game.Year, game.Weeknumber, DayOfWeek.Saturday)
            .AddHours(16)
            .AddMinutes(59);

        return TimeZoneInfo.ConvertTimeToUtc(saturdayLocal, dkZone);
    }

    private static DateTime GetDanishSaturdayCutoffUtc(Game game)
    {
        var dkZone = GetDanishTimeZoneForTests();

        var saturdayLocal = ISOWeek
            .ToDateTime(game.Year, game.Weeknumber, DayOfWeek.Saturday)
            .AddHours(17);

        return TimeZoneInfo.ConvertTimeToUtc(saturdayLocal, dkZone);
    }

    private static DateTime GetDanishSaturdayAfterCutoffUtc(Game game)
    {
        var dkZone = GetDanishTimeZoneForTests();

        var saturdayLocal = ISOWeek
            .ToDateTime(game.Year, game.Weeknumber, DayOfWeek.Saturday)
            .AddHours(18);

        return TimeZoneInfo.ConvertTimeToUtc(saturdayLocal, dkZone);
    }

    [Fact]
    public async Task CreateBoard_AllowsPurchase_BeforeSaturday1700_ForGameWeek()
    {
        var fakeTime = Assert.IsType<FakeTimeProvider>(timeProvider);

        var player = await CreateUniquePlayer("cutoff-before", balance: 100);
        var game   = await CreateUniqueGame(isActive: true);

        fakeTime.SetUtcNow(GetDanishSaturdayBeforeCutoffUtc(game));

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        var board = await boardService.CreateBoard(player.Id, dto);

        Assert.Equal(game.Id, board.Gameid);
        Assert.Equal(player.Id, board.Playerid);
    }

    [Fact]
    public async Task CreateBoard_Throws_AtExactlySaturday1700_ForGameWeek()
    {
        var fakeTime = Assert.IsType<FakeTimeProvider>(timeProvider);

        var player = await CreateUniquePlayer("cutoff-exact", balance: 100);
        var game   = await CreateUniqueGame(isActive: true);

        fakeTime.SetUtcNow(GetDanishSaturdayCutoffUtc(game));

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto));
    }

    [Fact]
    public async Task CreateBoard_Throws_AfterSaturday1700_ForGameWeek()
    {
        var fakeTime = Assert.IsType<FakeTimeProvider>(timeProvider);

        var player = await CreateUniquePlayer("cutoff-after", balance: 100);
        var game   = await CreateUniqueGame(isActive: true);

        fakeTime.SetUtcNow(GetDanishSaturdayAfterCutoffUtc(game));

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto));
    }
}
