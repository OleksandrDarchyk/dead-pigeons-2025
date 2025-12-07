// tests/GameServiceTests.cs
using api.Etc;
using api.Models.Requests;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = Bogus.ValidationException;


namespace tests;

/// <summary>
/// Service-level tests for <see cref="GameService"/>.
/// Verifies active game lookup, game history ordering, winning numbers logic,
/// repeating boards and player history mapping.
/// </summary>
public class GameServiceTests(
    IGameService gameService,
    MyDbContext ctx,
    TestTransactionScope transaction,
    TimeProvider timeProvider,
    ISeeder seeder,
    ITestOutputHelper outputHelper) : IAsyncLifetime
{
    /// <summary>
    /// Each test runs inside its own database transaction.
    /// The transaction is rolled back automatically after the test.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await transaction.BeginTransactionAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetActiveGame_Returns_ActiveGame_FromSeedData()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: seed database with initial data (should create exactly one active game)
        await seeder.Seed();

        // Act: use the service to retrieve the current active game
        var activeFromService = await gameService.GetActiveGame();

        // Assert: database must contain exactly one non-deleted active game,
        // and it must be the same game returned by the service
        var activeFromDb = await ctx.Games
            .Where(g => g.Deletedat == null && g.Isactive)
            .SingleAsync(ct);

        Assert.Equal(activeFromDb.Id, activeFromService.Id);
        Assert.True(activeFromService.Isactive);

        // Helpful output for debugging test failures
        outputHelper.WriteLine(
            $"[GameServiceTests] Active game: {activeFromService.Id}, year={activeFromService.Year}, week={activeFromService.Weeknumber}"
        );
    }

    [Fact]
    public async Task GetActiveGame_Throws_When_NoActiveGameExists()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: seed normal data and then manually deactivate all games
        await seeder.Seed();

        foreach (var g in ctx.Games)
        {
            g.Isactive = false;
        }

        await ctx.SaveChangesAsync(ct);

        // Act + Assert: service should fail with a domain validation error
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.GetActiveGame());
    }

    [Fact]
    public async Task GetGamesHistory_Returns_NonDeletedGames_NewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: seed initial data and soft-delete one game
        await seeder.Seed();

        var toDelete = await ctx.Games.FirstAsync(ct);
        toDelete.Deletedat = timeProvider.GetUtcNow().UtcDateTime;
        await ctx.SaveChangesAsync(ct);

        // Act
        var history = await gameService.GetGamesHistory();

        // Assert: soft-deleted games must not be included
        Assert.DoesNotContain(history, g => g.Deletedat != null);

        // And ordering must be newest first: Year desc, then Weeknumber desc
        var ordered = history
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Weeknumber)
            .Select(g => g.Id)
            .ToList();

        Assert.Equal(ordered, history.Select(g => g.Id).ToList());
    }

    [Fact]
    public async Task SetWinningNumbers_ClosesGame_ActivatesNext_CreatesRepeats_AndReturnsSummary()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: create a player, two games (current + next) and two boards
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var random = new Random();

        var player = new Player
        {
            Id         = Guid.NewGuid().ToString(),
            Fullname   = "Test Player",
            Email      = $"player-{Guid.NewGuid()}@gameservice.test",
            Phone      = "12345678",
            Isactive   = true,
            Activatedat = now,
            Createdat  = now,
            Deletedat  = null
        };
        ctx.Players.Add(player);

        var currentGame = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = now.Year + random.Next(0, 5),
            Weeknumber = random.Next(1, 52),
            Isactive   = true,
            Deletedat  = null,
            Createdat  = now
        };

        var nextGame = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = currentGame.Year + 1,
            Weeknumber = random.Next(1, 52),
            Isactive   = false,
            Deletedat  = null,
            Createdat  = now
        };

        ctx.Games.AddRange(currentGame, nextGame);

        // Repeating winning board (3 weeks prepaid)
        var winningBoard = new Board
        {
            Id          = Guid.NewGuid().ToString(),
            Playerid    = player.Id,
            Gameid      = currentGame.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price       = 20 * 3,   // prepaid for 3 weeks
            Iswinning   = false,
            Repeatweeks = 3,
            Repeatactive = true,
            Createdat   = now,
            Deletedat   = null
        };

        // Non-repeating losing board
        var losingBoard = new Board
        {
            Id          = Guid.NewGuid().ToString(),
            Playerid    = player.Id,
            Gameid      = currentGame.Id,
            Numbers     = new[] { 6, 7, 8, 9, 10 }.ToList(),
            Price       = 20,
            Iswinning   = false,
            Repeatweeks = 1,
            Repeatactive = false,
            Createdat   = now,
            Deletedat   = null
        };

        ctx.Boards.AddRange(winningBoard, losingBoard);
        await ctx.SaveChangesAsync(ct);

        // Act: set winning numbers in a different order (order must not matter)
        var request = new SetWinningNumbersRequestDto
        {
            GameId        = currentGame.Id,
            WinningNumbers = new[] { 3, 1, 2 }
        };

        var summary = await gameService.SetWinningNumbers(request);

        // Reload games with boards from the database
        var currentGameFromDb = await ctx.Games
            .Include(g => g.Boards)
            .SingleAsync(g => g.Id == currentGame.Id, ct);

        var nextGameFromDb = await ctx.Games
            .Include(g => g.Boards)
            .SingleAsync(g => g.Id == nextGame.Id, ct);

        // Assert: current game is closed with correct winning numbers
        Assert.False(currentGameFromDb.Isactive);
        Assert.NotNull(currentGameFromDb.Closedat);
        Assert.Equal(new[] { 1, 2, 3 }, currentGameFromDb.Winningnumbers!.OrderBy(n => n).ToArray());

        // The board that contains all three winning numbers must be marked as winning
        var winningBoardFromDb = currentGameFromDb.Boards.Single(b => b.Id == winningBoard.Id);
        Assert.True(winningBoardFromDb.Iswinning);

        // Next game must be activated
        Assert.True(nextGameFromDb.Isactive);

        // Exactly one repeating board should be created for the next game
        var repeatedBoards = nextGameFromDb.Boards.ToList();
        Assert.Single(repeatedBoards);

        var repeated = repeatedBoards.Single();
        Assert.Equal(2, repeated.Repeatweeks);       // 3 -> 2 remaining
        Assert.True(repeated.Repeatactive);          // still more than 1 week left
        Assert.Equal(0, repeated.Price);             // future repeats must not charge again
        Assert.Equal(winningBoard.Numbers.OrderBy(n => n), repeated.Numbers.OrderBy(n => n));

        // Assert: summary DTO must match the closed game and weekly revenue rules
        Assert.Equal(currentGame.Id, summary.GameId);
        Assert.Equal(currentGame.Weeknumber, summary.WeekNumber);
        Assert.Equal(currentGame.Year, summary.Year);
        Assert.Equal(new[] { 1, 2, 3 }, summary.WinningNumbers.OrderBy(n => n).ToArray());
        Assert.Equal(2, summary.TotalBoards);
        Assert.Equal(1, summary.WinningBoards);

        // Weekly digital revenue: both boards have 5 numbers -> 20 + 20 = 40
        Assert.Equal(40, summary.DigitalRevenue);

        outputHelper.WriteLine(
            $"[GameServiceTests] Summary: game={summary.GameId}, boards={summary.TotalBoards}, winners={summary.WinningBoards}, revenue={summary.DigitalRevenue}"
        );
    }

    [Fact]
    public async Task SetWinningNumbers_Throws_When_NotThreeDistinctNumbers()
    {
        // Arrange: use seeded active game
        await seeder.Seed();
        var game = await gameService.GetActiveGame();

        var dto = new SetWinningNumbersRequestDto
        {
            GameId = game.Id,
            WinningNumbers = new[] { 1, 1, 2 } // duplicate "1"
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.SetWinningNumbers(dto));
    }

    [Fact]
    public async Task SetWinningNumbers_Throws_When_NumberOutOfRange()
    {
        // Arrange: use seeded active game
        await seeder.Seed();
        var game = await gameService.GetActiveGame();

        var dto = new SetWinningNumbersRequestDto
        {
            GameId = game.Id,
            WinningNumbers = new[] { 0, 1, 2 } // 0 is outside [1;16]
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.SetWinningNumbers(dto));
    }

    [Fact]
    public async Task GetPlayerHistory_Throws_When_EmailMissing()
    {
        // Empty email should not be allowed
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.GetPlayerHistory(""));
    }

    [Fact]
    public async Task GetPlayerHistory_Throws_When_PlayerNotFound()
    {
        // Non-existing email should result in a domain error
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.GetPlayerHistory("does-not-exist@gameservice.test"));
    }

    [Fact]
    public async Task GetPlayerHistory_Returns_OnlyPlayersBoards_OrderedNewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var random = new Random();

        // Arrange: one player with boards in two games, and another player with its own board
        var player = new Player
        {
            Id         = Guid.NewGuid().ToString(),
            Fullname   = "History Player",
            Email      = $"history-{Guid.NewGuid()}@gameservice.test",
            Phone      = "87654321",
            Isactive   = true,
            Activatedat = now,
            Createdat  = now,
            Deletedat  = null
        };

        var otherPlayer = new Player
        {
            Id         = Guid.NewGuid().ToString(),
            Fullname   = "Other Player",
            Email      = $"other-{Guid.NewGuid()}@gameservice.test",
            Phone      = "11111111",
            Isactive   = true,
            Activatedat = now,
            Createdat  = now,
            Deletedat  = null
        };

        ctx.Players.AddRange(player, otherPlayer);

        var olderGame = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = now.Year - 1,
            Weeknumber = random.Next(1, 52),
            Isactive   = false,
            Closedat   = now.AddDays(-7),
            Deletedat  = null,
            Createdat  = now.AddDays(-14)
        };

        var newerGame = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = now.Year,
            Weeknumber = random.Next(1, 52),
            Isactive   = false,
            Closedat   = now,
            Deletedat  = null,
            Createdat  = now.AddDays(-1)
        };

        ctx.Games.AddRange(olderGame, newerGame);
        await ctx.SaveChangesAsync(ct);

        var olderBoard = new Board
        {
            Id          = Guid.NewGuid().ToString(),
            Playerid    = player.Id,
            Gameid      = olderGame.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price       = 20,
            Iswinning   = false,
            Repeatweeks = 1,
            Repeatactive = false,
            Createdat   = now.AddDays(-10),
            Deletedat   = null
        };

        var newerBoard = new Board
        {
            Id          = Guid.NewGuid().ToString(),
            Playerid    = player.Id,
            Gameid      = newerGame.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5, 6 }.ToList(),
            Price       = 40,
            Iswinning   = true,
            Repeatweeks = 1,
            Repeatactive = false,
            Createdat   = now.AddDays(-3),
            Deletedat   = null
        };

        var otherBoard = new Board
        {
            Id          = Guid.NewGuid().ToString(),
            Playerid    = otherPlayer.Id,
            Gameid      = newerGame.Id,
            Numbers     = new[] { 7, 8, 9, 10, 11 }.ToList(),
            Price       = 20,
            Iswinning   = false,
            Repeatweeks = 1,
            Repeatactive = false,
            Createdat   = now,
            Deletedat   = null
        };

        ctx.Boards.AddRange(olderBoard, newerBoard, otherBoard);
        await ctx.SaveChangesAsync(ct);

        // Act
        var history = await gameService.GetPlayerHistory(player.Email);

        // Assert: only boards for this player are included, ordered by game (newer -> older)
        Assert.Equal(2, history.Count);
        Assert.Equal(new[] { newerGame.Id, olderGame.Id }, history.Select(h => h.GameId).ToArray());

        // And weekly prices in history must reflect number of fields (5 -> 20, 6 -> 40)
        var prices = history.Select(h => h.Price).ToArray();
        Assert.Contains(20, prices);
        Assert.Contains(40, prices);
    }
}
