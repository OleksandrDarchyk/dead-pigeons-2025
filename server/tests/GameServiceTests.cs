// server/tests/GameServiceTests.cs
using api.Models.Requests;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using ValidationException = Bogus.ValidationException;

namespace tests;

/// <summary>
/// Service-level tests for GameService.
/// Covers:
/// - GetActiveGame
/// - GetGamesHistory ordering + soft delete
/// - SetWinningNumbers: closes current game, marks winners, activates next game, repeats boards
/// - GetPlayerHistory mapping + ordering + error cases
/// </summary>
public class GameServiceTests(
    IGameService gameService,
    MyDbContext ctx,
    TestTransactionScope transaction,
    TimeProvider timeProvider,
    ITestOutputHelper output) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await transaction.BeginTransactionAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        transaction.Dispose(); // guarantee rollback
        return ValueTask.CompletedTask;
    }

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------

    private async Task<Game> CreateGame(int year, int week, bool isActive)
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var game = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = year,
            Weeknumber = week,
            Isactive   = isActive,
            Createdat  = now,
            Closedat   = null,
            Winningnumbers = null,
            Deletedat  = null
        };

        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);
        return game;
    }

    private async Task<(Game current, Game next)> CreateCurrentAndNextGame()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Use far future years/weeks to avoid collisions with any seeded data.
        var year = now.Year + 30;
        var currentWeek = 10;
        var nextWeek = 11;

        // Ensure there is exactly 1 active game, and at least 1 future inactive game.
        var current = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = year,
            Weeknumber = currentWeek,
            Isactive   = true,
            Createdat  = now,
            Closedat   = null,
            Winningnumbers = null,
            Deletedat  = null
        };

        var next = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = year,
            Weeknumber = nextWeek,
            Isactive   = false,
            Createdat  = now,
            Closedat   = null,
            Winningnumbers = null,
            Deletedat  = null
        };

        ctx.Games.AddRange(current, next);
        await ctx.SaveChangesAsync(ct);

        return (current, next);
    }

    private async Task<Player> CreatePlayer(string email, bool isActive = true)
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var p = new Player
        {
            Id          = Guid.NewGuid().ToString(),
            Fullname    = "Test Player",
            Email       = email,
            Phone       = "12345678",
            Isactive    = isActive,
            Activatedat = isActive ? now : null,
            Createdat   = now,
            Deletedat   = null
        };

        ctx.Players.Add(p);
        await ctx.SaveChangesAsync(ct);
        return p;
    }

    private async Task<Transaction> AddApprovedTransaction(Player player, int amount)
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var tx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Amount          = amount,
            Status          = "Approved",
            Mobilepaynumber = $"TX-{now.Ticks}-{Random.Shared.Next(1000,9999)}",
            Createdat       = now,
            Approvedat      = now,
            Rejectionreason = null,
            Deletedat       = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);
        return tx;
    }

    private async Task<Board> AddBoard(
        Player player,
        Game game,
        int[] numbers,
        int price,
        bool repeatActive,
        int repeatWeeks)
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var b = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = numbers.ToList(),
            Price        = price,
            Iswinning    = false,
            Repeatactive = repeatActive,
            Repeatweeks  = repeatWeeks,
            Createdat    = now,
            Deletedat    = null
        };

        ctx.Boards.Add(b);
        await ctx.SaveChangesAsync(ct);
        return b;
    }

    // ------------------------------------------------------------
    // GetActiveGame
    // ------------------------------------------------------------

    [Fact]
    public async Task GetActiveGame_Returns_ActiveGame()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange: create one active + one inactive
        var year = now.Year + 40;
        var active = await CreateGame(year, 1, isActive: true);
        await CreateGame(year, 2, isActive: false);

        // Act
        var result = await gameService.GetActiveGame();

        // Assert
        Assert.Equal(active.Id, result.Id);
        Assert.True(result.Isactive);
        Assert.Null(result.Deletedat);
    }

    [Fact]
    public async Task GetActiveGame_Throws_When_NoActiveGameExists()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange: only inactive games
        var year = now.Year + 41;
        await CreateGame(year, 1, isActive: false);
        await CreateGame(year, 2, isActive: false);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.GetActiveGame());
    }

    // ------------------------------------------------------------
    // GetGamesHistory
    // ------------------------------------------------------------

    [Fact]
    public async Task GetGamesHistory_Returns_NonDeletedGames_NewestFirst()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange: create multiple games + soft-delete one
        var year = now.Year + 42;

        var g1 = await CreateGame(year, 1, isActive: false);
        var g2 = await CreateGame(year, 2, isActive: false);
        var g3 = await CreateGame(year, 3, isActive: true);

        g2.Deletedat = now;
        await ctx.SaveChangesAsync(ct);

        // Act
        var history = await gameService.GetGamesHistory();

        // Assert: excludes deleted
        Assert.DoesNotContain(history, g => g.Deletedat != null);

        // Assert ordering: Year desc, Week desc
        var orderedIds = history
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Weeknumber)
            .Select(g => g.Id)
            .ToList();

        Assert.Equal(orderedIds, history.Select(g => g.Id).ToList());

        // sanity: contains g3 and g1, not g2
        Assert.Contains(history, g => g.Id == g3.Id);
        Assert.Contains(history, g => g.Id == g1.Id);
        Assert.DoesNotContain(history, g => g.Id == g2.Id);
    }

    // ------------------------------------------------------------
    // SetWinningNumbers
    // ------------------------------------------------------------

    [Fact]
    public async Task SetWinningNumbers_ClosesGame_ActivatesNext_MarksWinners_CreatesRepeatBoard_AndReturnsSummary()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var (current, next) = await CreateCurrentAndNextGame();

        var player = await CreatePlayer($"player-{Guid.NewGuid()}@test.local", isActive: true);

        // Give balance so repeating can be created in next game
        await AddApprovedTransaction(player, amount: 100);

        // Board that will WIN and repeats:
        // Winning numbers will be {1,2,3}, board has {1,2,3,4,5} -> win
        var winningBoard = await AddBoard(
            player, current,
            numbers: new[] { 1, 2, 3, 4, 5 },
            price: 20,
            repeatActive: true,
            repeatWeeks: 3);

        // Board that will LOSE and does not repeat
        await AddBoard(
            player, current,
            numbers: new[] { 6, 7, 8, 9, 10 },
            price: 20,
            repeatActive: false,
            repeatWeeks: 0);

        // Act
        var dto = new SetWinningNumbersRequestDto
        {
            GameId = current.Id,
            WinningNumbers = new[] { 3, 1, 2 } // shuffled on purpose
        };

        var summary = await gameService.SetWinningNumbers(dto);

        // Reload
        var currentDb = await ctx.Games
            .Include(g => g.Boards)
            .SingleAsync(g => g.Id == current.Id, ct);

        var nextDb = await ctx.Games
            .Include(g => g.Boards)
            .SingleAsync(g => g.Id == next.Id, ct);

        // Assert: current closed + numbers sorted
        Assert.False(currentDb.Isactive);
        Assert.NotNull(currentDb.Closedat);
        Assert.Equal(new[] { 1, 2, 3 }, currentDb.Winningnumbers!.OrderBy(x => x).ToArray());

        // Winning board marked
        var winBoardDb = currentDb.Boards.Single(b => b.Id == winningBoard.Id);
        Assert.True(winBoardDb.Iswinning);

        // Next active
        Assert.True(nextDb.Isactive);

        // Repeat board created (because repeatWeeks>0 and enough balance)
        Assert.Single(nextDb.Boards);
        var repeated = nextDb.Boards.Single();

        Assert.Equal(player.Id, repeated.Playerid);
        Assert.Equal(nextDb.Id, repeated.Gameid);
        Assert.Equal(20, repeated.Price);          // weekly price charged again
        Assert.Equal(2, repeated.Repeatweeks);     // 3 -> 2 left
        Assert.True(repeated.Repeatactive);
        Assert.Equal(
            winningBoard.Numbers.OrderBy(n => n),
            repeated.Numbers.OrderBy(n => n)
        );

        // Summary checks
        Assert.Equal(current.Id, summary.GameId);
        Assert.Equal(current.Year, summary.Year);
        Assert.Equal(current.Weeknumber, summary.WeekNumber);
        Assert.Equal(new[] { 1, 2, 3 }, summary.WinningNumbers.OrderBy(x => x).ToArray());
        Assert.Equal(2, summary.TotalBoards);
        Assert.Equal(1, summary.WinningBoards);
        Assert.Equal(40, summary.DigitalRevenue); // 20 + 20

        output.WriteLine($"[Summary] game={summary.GameId}, winners={summary.WinningBoards}, revenue={summary.DigitalRevenue}");
    }

    [Fact]
    public async Task SetWinningNumbers_Throws_When_NotThreeDistinctNumbers()
    {
        var (current, next) = await CreateCurrentAndNextGame();

        var dto = new SetWinningNumbersRequestDto
        {
            GameId = current.Id,
            WinningNumbers = new[] { 1, 1, 2 }
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.SetWinningNumbers(dto));
    }

    [Fact]
    public async Task SetWinningNumbers_Throws_When_NumberOutOfRange()
    {
        var (current, next) = await CreateCurrentAndNextGame();

        var dto = new SetWinningNumbersRequestDto
        {
            GameId = current.Id,
            WinningNumbers = new[] { 0, 1, 2 }
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.SetWinningNumbers(dto));
    }

    [Fact]
    public async Task SetWinningNumbers_Throws_When_GameNotFound()
    {
        var dto = new SetWinningNumbersRequestDto
        {
            GameId = Guid.NewGuid().ToString(),
            WinningNumbers = new[] { 1, 2, 3 }
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.SetWinningNumbers(dto));
    }

    [Fact]
    public async Task SetWinningNumbers_Throws_When_GameAlreadyFinished()
    {
        var (current, next) = await CreateCurrentAndNextGame();

        current.Isactive = false;
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new SetWinningNumbersRequestDto
        {
            GameId = current.Id,
            WinningNumbers = new[] { 1, 2, 3 }
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.SetWinningNumbers(dto));
    }

    [Fact]
    public async Task SetWinningNumbers_Throws_When_WinningNumbersAlreadySet()
    {
        var ct = TestContext.Current.CancellationToken;
        var (current, next) = await CreateCurrentAndNextGame();

        current.Winningnumbers = new List<int> { 1, 2, 3 };
        await ctx.SaveChangesAsync(ct);

        var dto = new SetWinningNumbersRequestDto
        {
            GameId = current.Id,
            WinningNumbers = new[] { 1, 2, 3 }
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.SetWinningNumbers(dto));
    }

    // ------------------------------------------------------------
    // GetPlayerHistory
    // ------------------------------------------------------------

    [Fact]
    public async Task GetPlayerHistory_Throws_When_EmailMissing()
    {
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.GetPlayerHistory(""));
    }

    [Fact]
    public async Task GetPlayerHistory_Throws_When_PlayerNotFound()
    {
        await Assert.ThrowsAsync<ValidationException>(
            async () => await gameService.GetPlayerHistory("nope@test.local"));
    }

    [Fact]
    public async Task GetPlayerHistory_Returns_OnlyPlayersBoards_OrderedNewestFirst_ByGame()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreatePlayer($"history-{Guid.NewGuid()}@test.local", isActive: true);
        var other  = await CreatePlayer($"other-{Guid.NewGuid()}@test.local", isActive: true);

        var olderGame = await CreateGame(year: now.Year + 50, week: 1, isActive: false);
        olderGame.Closedat = now.AddDays(-7);

        var newerGame = await CreateGame(year: now.Year + 50, week: 2, isActive: false);
        newerGame.Closedat = now;

        await ctx.SaveChangesAsync(ct);

        await AddBoard(player, olderGame, new[] { 1, 2, 3, 4, 5 }, price: 20, repeatActive: false, repeatWeeks: 0);
        await AddBoard(player, newerGame, new[] { 1, 2, 3, 4, 5, 6 }, price: 40, repeatActive: false, repeatWeeks: 0);
        await AddBoard(other,  newerGame, new[] { 7, 8, 9, 10, 11 }, price: 20, repeatActive: false, repeatWeeks: 0);

        // Act
        var history = await gameService.GetPlayerHistory(player.Email);

        // Assert: only player's 2 items, newest game first
        Assert.Equal(2, history.Count);
        Assert.Equal(new[] { newerGame.Id, olderGame.Id }, history.Select(h => h.GameId).ToArray());
        Assert.All(history, h => Assert.NotNull(h.Numbers));

        // Prices reflect weekly prices (5->20, 6->40)
        Assert.Contains(history, h => h.Price == 20);
        Assert.Contains(history, h => h.Price == 40);
    }
}
