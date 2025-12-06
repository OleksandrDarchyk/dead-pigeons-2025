// server/tests/GameServiceTests.cs

using api.Etc;
using api.Models.Requests;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = Bogus.ValidationException;


namespace tests;

public class GameServiceTests(
    IGameService gameService,
    MyDbContext ctx,
    ISeeder seeder,
    ITestOutputHelper outputHelper)
{
    // ================================
    // GetActiveGame
    // ================================

    [Fact]
    public async Task GetActiveGame_Returns_ActiveGame_FromSeedData()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: seed a realistic game setup
        await seeder.Seed();

        // Act
        var activeFromService = await gameService.GetActiveGame();

        // Assert: there should be exactly one active game in the database
        var activeFromDb = await ctx.Games
            .Where(g => g.Deletedat == null && g.Isactive)
            .SingleAsync(ct);

        outputHelper.WriteLine($"Active game from service: {activeFromService.Id}");

        Assert.Equal(activeFromDb.Id, activeFromService.Id);
        Assert.True(activeFromService.Isactive);
        Assert.Null(activeFromService.Deletedat);
    }

    [Fact]
    public async Task GetActiveGame_Throws_When_NoActiveGameExists()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: seed data and then break the invariant by disabling all games
        await seeder.Seed();

        foreach (var g in ctx.Games)
        {
            g.Isactive = false;
        }

        await ctx.SaveChangesAsync(ct);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.GetActiveGame()
        );
    }

    // ================================
    // GetGamesHistory
    // ================================

    [Fact]
    public async Task GetGamesHistory_Returns_NonDeletedGames_NewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: seed data and soft-delete one game
        await seeder.Seed();

        var toDelete = await ctx.Games.FirstAsync(ct);
        toDelete.Deletedat = DateTime.UtcNow;
        await ctx.SaveChangesAsync(ct);

        // Act
        var history = await gameService.GetGamesHistory();

        // Assert: no soft-deleted games are returned
        Assert.DoesNotContain(history, g => g.Deletedat != null);

        // Assert: games are ordered by Year desc, Weeknumber desc
        var ordered = history
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Weeknumber)
            .Select(g => g.Id)
            .ToList();

        Assert.Equal(ordered, history.Select(g => g.Id).ToList());
    }

    // ================================
    // SetWinningNumbers – happy path
    // ================================

    [Fact]
    public async Task SetWinningNumbers_ClosesGame_ActivatesNext_CreatesRepeats_AndReturnsSummary()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: minimal custom scenario without seeder

        // 1) Player
        var player = new Player
        {
            Id          = Guid.NewGuid().ToString(),
            Fullname    = "Test Player",
            Email       = "player@gameservice.test",
            Phone       = "12345678",
            Isactive    = true,
            Activatedat = DateTime.UtcNow,
            Createdat   = DateTime.UtcNow,
            Deletedat   = null
        };
        ctx.Players.Add(player);

        // 2) Current active game + next inactive game
        var currentGame = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = 2025,
            Weeknumber = 1,
            Isactive   = true,
            Deletedat  = null,
            Createdat  = DateTime.UtcNow
        };

        var nextGame = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = 2025,
            Weeknumber = 2,
            Isactive   = false,
            Deletedat  = null,
            Createdat  = DateTime.UtcNow
        };

        ctx.Games.AddRange(currentGame, nextGame);

        // 3) Boards in current game

        // Winning board with repeat for 3 weeks
        var winningBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = currentGame.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(), // 5 numbers → weekly price 20
            Price        = 20 * 3,                           // prepaid for 3 weeks
            Iswinning    = false,
            Repeatweeks  = 3,
            Repeatactive = true,
            Createdat    = DateTime.UtcNow,
            Deletedat    = null
        };

        // Losing board without repeat
        var losingBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = currentGame.Id,
            Numbers      = new[] { 6, 7, 8, 9, 10 }.ToList(), // 5 numbers → weekly price 20
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 1,
            Repeatactive = false,
            Createdat    = DateTime.UtcNow,
            Deletedat    = null
        };

        ctx.Boards.AddRange(winningBoard, losingBoard);

        await ctx.SaveChangesAsync(ct);

        // Winning numbers in random order (service should sort them)
        var request = new SetWinningNumbersRequestDto
        {
            GameId         = currentGame.Id,
            WinningNumbers = new[] { 3, 1, 2 }
        };

        // Act
        var summary = await gameService.SetWinningNumbers(request);

        // Reload games from the database with boards
        var currentGameFromDb = await ctx.Games
            .Include(g => g.Boards)
            .SingleAsync(g => g.Id == currentGame.Id, ct);

        var nextGameFromDb = await ctx.Games
            .Include(g => g.Boards)
            .SingleAsync(g => g.Id == nextGame.Id, ct);

        var boardsForCurrent = currentGameFromDb.Boards
            .Where(b => b.Deletedat == null)
            .ToList();

        // Assert: current game is closed and has winning numbers
        Assert.False(currentGameFromDb.Isactive);
        Assert.NotNull(currentGameFromDb.Closedat);

        var winningNumbersSorted = currentGameFromDb.Winningnumbers!.OrderBy(n => n).ToArray();
        Assert.Equal(new[] { 1, 2, 3 }, winningNumbersSorted);

        // Assert: boards are marked as winning / losing correctly
        var winningBoardFromDb = boardsForCurrent.Single(b => b.Id == winningBoard.Id);
        var losingBoardFromDb  = boardsForCurrent.Single(b => b.Id == losingBoard.Id);

        Assert.True(winningBoardFromDb.Iswinning);
        Assert.False(losingBoardFromDb.Iswinning);

        // Assert: next game becomes active
        Assert.True(nextGameFromDb.Isactive);

        // Assert: a new repeating board is created in the next game
        var repeatedBoards = nextGameFromDb.Boards.ToList();
        Assert.Single(repeatedBoards);

        var repeated = repeatedBoards.Single();
        Assert.Equal(
            winningBoard.Numbers.OrderBy(n => n),
            repeated.Numbers.OrderBy(n => n)
        );
        Assert.Equal(0, repeated.Price);          // do not charge again
        Assert.Equal(2, repeated.Repeatweeks);    // 3 - 1 = 2 weeks remaining
        Assert.True(repeated.Repeatactive);       // still active for future rounds

        // Assert: summary matches the actual game data
        Assert.Equal(currentGame.Id, summary.GameId);
        Assert.Equal(currentGame.Year, summary.Year);
        Assert.Equal(currentGame.Weeknumber, summary.WeekNumber);
        Assert.Equal(new[] { 1, 2, 3 }, summary.WinningNumbers);
        Assert.Equal(2, summary.TotalBoards);
        Assert.Equal(1, summary.WinningBoards);

        // Both boards have 5 numbers → weekly price 20 each → total 40
        Assert.Equal(40, summary.DigitalRevenue);

        outputHelper.WriteLine($"Summary revenue: {summary.DigitalRevenue}");
    }

    // ================================
    // SetWinningNumbers – validation
    // ================================

    [Fact]
    public async Task SetWinningNumbers_Throws_When_NotThreeDistinctNumbers()
    {
        // Arrange: use seeded active game
        await seeder.Seed();
        var game = await gameService.GetActiveGame();

        var dto = new SetWinningNumbersRequestDto
        {
            GameId         = game.Id,
            WinningNumbers = new[] { 1, 1, 2 } // not 3 distinct numbers
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.SetWinningNumbers(dto)
        );
    }

    [Fact]
    public async Task SetWinningNumbers_Throws_When_NumberOutOfRange()
    {
        // Arrange: use seeded active game
        await seeder.Seed();
        var game = await gameService.GetActiveGame();

        var dto = new SetWinningNumbersRequestDto
        {
            GameId         = game.Id,
            WinningNumbers = new[] { 0, 1, 2 } // 0 is outside [1;16]
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.SetWinningNumbers(dto)
        );
    }

    // ================================
    // GetPlayerHistory
    // ================================

    [Fact]
    public async Task GetPlayerHistory_Throws_When_EmailMissing()
    {
        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.GetPlayerHistory("")
        );
    }

    [Fact]
    public async Task GetPlayerHistory_Throws_When_PlayerNotFound()
    {
        const string email = "does-not-exist@gameservice.test";

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.GetPlayerHistory(email)
        );
    }

    [Fact]
    public async Task GetPlayerHistory_Returns_OnlyPlayersBoards_OrderedNewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: build a small custom dataset

        var player = new Player
        {
            Id          = Guid.NewGuid().ToString(),
            Fullname    = "History Player",
            Email       = "history@gameservice.test",
            Phone       = "87654321",
            Isactive    = true,
            Activatedat = DateTime.UtcNow,
            Createdat   = DateTime.UtcNow,
            Deletedat   = null
        };

        var otherPlayer = new Player
        {
            Id          = Guid.NewGuid().ToString(),
            Fullname    = "Other Player",
            Email       = "other@gameservice.test",
            Phone       = "11111111",
            Isactive    = true,
            Activatedat = DateTime.UtcNow,
            Createdat   = DateTime.UtcNow,
            Deletedat   = null
        };

        ctx.Players.AddRange(player, otherPlayer);

        var olderGame = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = 2024,
            Weeknumber = 52,
            Isactive   = false,
            Closedat   = DateTime.UtcNow.AddDays(-7),
            Deletedat  = null,
            Createdat  = DateTime.UtcNow.AddDays(-14)
        };

        var newerGame = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = 2025,
            Weeknumber = 1,
            Isactive   = false,
            Closedat   = DateTime.UtcNow,
            Deletedat  = null,
            Createdat  = DateTime.UtcNow.AddDays(-1)
        };

        ctx.Games.AddRange(olderGame, newerGame);
        await ctx.SaveChangesAsync(ct);

        // Two boards for our target player
        var olderBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = olderGame.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(), // 5 numbers → weekly price 20
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 1,
            Repeatactive = false,
            Createdat    = DateTime.UtcNow.AddDays(-10),
            Deletedat    = null
        };

        var newerBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = newerGame.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5, 6 }.ToList(), // 6 numbers → weekly price 40
            Price        = 40,
            Iswinning    = true,
            Repeatweeks  = 1,
            Repeatactive = false,
            Createdat    = DateTime.UtcNow.AddDays(-3),
            Deletedat    = null
        };

        // Board for another player (must not appear in history)
        var otherBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = otherPlayer.Id,
            Gameid       = newerGame.Id,
            Numbers      = new[] { 7, 8, 9, 10, 11 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 1,
            Repeatactive = false,
            Createdat    = DateTime.UtcNow,
            Deletedat    = null
        };

        ctx.Boards.AddRange(olderBoard, newerBoard, otherBoard);
        await ctx.SaveChangesAsync(ct);

        // Act
        var history = await gameService.GetPlayerHistory(player.Email);

        // Assert: only 2 boards for the target player
        Assert.Equal(2, history.Count);

        // Order: newest game (2025/1) first, then older (2024/52)
        Assert.Equal(
            new[] { newerGame.Id, olderGame.Id },
            history.Select(h => h.GameId).ToArray()
        );

        var first = history[0]; // newer board
        Assert.Equal(newerBoard.Id, first.BoardId);
        Assert.Equal(6, first.Numbers.Length);
        Assert.Equal(40, first.Price); // weekly price for 6 numbers → 40

        var second = history[1]; // older board
        Assert.Equal(olderBoard.Id, second.BoardId);
        Assert.Equal(5, second.Numbers.Length);
        Assert.Equal(20, second.Price); // weekly price for 5 numbers → 20
    }
}
