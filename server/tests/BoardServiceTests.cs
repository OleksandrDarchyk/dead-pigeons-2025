// server/tests/BoardServiceTests.cs

using System.Security.Claims;
using api.Models.Requests;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;


// Domain validation errors in services use Bogus.ValidationException
using ValidationException = Bogus.ValidationException;
// DTO attribute validation uses DataAnnotations.ValidationException
using DataAnnotationValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace tests;

public class BoardServiceTests(
    IBoardService boardService,
    MyDbContext ctx,
    ITestOutputHelper outputHelper)
{
    // Helper: create a simple active player
    private static Player CreateActivePlayer(string email)
    {
        var now = DateTime.UtcNow;

        return new Player
        {
            Id          = Guid.NewGuid().ToString(),
            Fullname    = "Test Player",
            Email       = email,
            Phone       = "12345678",
            Isactive    = true,
            Activatedat = now,
            Createdat   = now,
            Deletedat   = null
        };
    }

    // Helper: create a simple inactive player
    private static Player CreateInactivePlayer(string email)
    {
        var now = DateTime.UtcNow;

        return new Player
        {
            Id          = Guid.NewGuid().ToString(),
            Fullname    = "Inactive Player",
            Email       = email,
            Phone       = "12345678",
            Isactive    = false,
            Activatedat = null,
            Createdat   = now,
            Deletedat   = null
        };
    }

    // Helper: create a simple game
    private static Game CreateGame(int year, int weekNumber, bool isActive)
    {
        var now = DateTime.UtcNow;

        return new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = year,
            Weeknumber = weekNumber,
            Isactive   = isActive,
            Createdat  = now,
            Closedat   = null,
            Deletedat  = null
        };
    }

    // Helper: add a positive balance for a player via an approved transaction
    private static Transaction CreateApprovedTransaction(string playerId, int amount)
    {
        var now = DateTime.UtcNow;

        return new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = playerId,
            Amount          = amount,
            Status          = "Approved",
            Mobilepaynumber = "123456",
            Createdat       = now,
            Approvedat      = now,
            Deletedat       = null
        };
    }

    // ============================================================
    // CreateBoard – happy path
    // ============================================================

    [Fact]
    public async Task CreateBoard_Succeeds_When_PlayerHasEnoughBalance_AndValidNumbers()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: one active player, one active game, one approved transaction
        var player = CreateActivePlayer("board-happy@test.local");
        var game   = CreateGame(2025, 1, isActive: true);

        ctx.Players.Add(player);
        ctx.Games.Add(game);

        // 100 DKK approved balance
        var tx = CreateApprovedTransaction(player.Id, amount: 100);
        ctx.Transactions.Add(tx);

        await ctx.SaveChangesAsync(ct);

        // Board with 5 numbers => weekly price 20, no repeat => total cost 20
        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 5, 1, 4, 2, 3 }, // unsorted on purpose
            RepeatWeeks = 0
        };

        // Act
        var board = await boardService.CreateBoard(player.Id, dto);

        // Assert: basic properties
        Assert.False(string.IsNullOrWhiteSpace(board.Id));
        Assert.Equal(player.Id, board.Playerid);
        Assert.Equal(game.Id, board.Gameid);
        Assert.Null(board.Deletedat);
        Assert.False(board.Iswinning);

        // Numbers are stored sorted
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, board.Numbers.OrderBy(n => n));

        // Price logic: 5 numbers => 20 DKK, no repeat => 1 week => 20 total
        Assert.Equal(20, board.Price);
        Assert.Equal(0, board.Repeatweeks);
        Assert.False(board.Repeatactive);

        // Board is saved to database with the same values
        var fromDb = await ctx.Boards.SingleAsync(b => b.Id == board.Id, ct);
        Assert.Equal(board.Price, fromDb.Price);
        Assert.Equal(board.Repeatweeks, fromDb.Repeatweeks);

        // Balance formula: sum(approved transactions) - sum(board.Price)
        var approvedAmount = await ctx.Transactions
            .Where(t =>
                t.Playerid == player.Id &&
                t.Deletedat == null &&
                t.Status == "Approved")
            .SumAsync(t => (int?)t.Amount, ct) ?? 0;

        var spentOnBoards = await ctx.Boards
            .Where(b => b.Playerid == player.Id && b.Deletedat == null)
            .SumAsync(b => (int?)b.Price, ct) ?? 0;

        var balance = approvedAmount - spentOnBoards;

        // 100 - 20 = 80 should remain
        Assert.Equal(80, balance);

        outputHelper.WriteLine($"Remaining balance after board purchase: {balance}");
    }

    // ============================================================
    // CreateBoard – balance & domain rules
    // ============================================================

    [Fact]
    public async Task CreateBoard_Throws_When_PlayerHasNotEnoughBalance()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: active player + active game, but no approved transactions
        var player = CreateActivePlayer("no-balance@test.local");
        var game   = CreateGame(2025, 1, isActive: true);

        ctx.Players.Add(player);
        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 1 // cost 20 * 1 = 20, balance is 0
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto)
        );
    }

    [Fact]
    public async Task CreateBoard_Throws_When_NumbersCountIsInvalid()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("count-invalid@test.local");
        var game   = CreateGame(2025, 1, isActive: true);

        ctx.Players.Add(player);
        ctx.Games.Add(game);

        // Give a large balance so only the numbers rule is tested
        var tx = CreateApprovedTransaction(player.Id, amount: 1000);
        ctx.Transactions.Add(tx);

        await ctx.SaveChangesAsync(ct);

        // Only 4 numbers -> should fail DTO MinLength (5)
        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<DataAnnotationValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto)
        );
    }

    [Fact]
    public async Task CreateBoard_Throws_When_NumbersAreNotDistinct()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("duplicate-numbers@test.local");
        var game   = CreateGame(2025, 1, isActive: true);

        ctx.Players.Add(player);
        ctx.Games.Add(game);

        var tx = CreateApprovedTransaction(player.Id, amount: 1000);
        ctx.Transactions.Add(tx);

        await ctx.SaveChangesAsync(ct);

        // Duplicate "1"
        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 1, 2, 3, 4 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto)
        );
    }

    [Fact]
    public async Task CreateBoard_Throws_When_NumberOutOfRange()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("range-invalid@test.local");
        var game   = CreateGame(2025, 1, isActive: true);

        ctx.Players.Add(player);
        ctx.Games.Add(game);

        var tx = CreateApprovedTransaction(player.Id, amount: 1000);
        ctx.Transactions.Add(tx);

        await ctx.SaveChangesAsync(ct);

        // 17 is outside [1;16]
        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 17 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto)
        );
    }

    [Fact]
    public async Task CreateBoard_Throws_When_PlayerNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        var game = CreateGame(2025, 1, isActive: true);
        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        var unknownPlayerId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(unknownPlayerId, dto)
        );
    }

    [Fact]
    public async Task CreateBoard_Throws_When_PlayerIsInactive()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateInactivePlayer("inactive@test.local");
        var game   = CreateGame(2025, 1, isActive: true);

        ctx.Players.Add(player);
        ctx.Games.Add(game);

        // Give big balance so only IsActive rule matters
        var tx = CreateApprovedTransaction(player.Id, amount: 1000);
        ctx.Transactions.Add(tx);

        await ctx.SaveChangesAsync(ct);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto)
        );
    }

    [Fact]
    public async Task CreateBoard_Throws_When_GameNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("no-game@test.local");
        ctx.Players.Add(player);

        var tx = CreateApprovedTransaction(player.Id, amount: 1000);
        ctx.Transactions.Add(tx);

        await ctx.SaveChangesAsync(ct);

        var dto = new CreateBoardRequestDto
        {
            GameId      = Guid.NewGuid().ToString(), // unknown game
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto)
        );
    }

    [Fact]
    public async Task CreateBoard_Throws_When_GameIsInactive()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("inactive-game@test.local");
        var game   = CreateGame(2025, 1, isActive: false); // inactive game

        ctx.Players.Add(player);
        ctx.Games.Add(game);

        var tx = CreateApprovedTransaction(player.Id, amount: 1000);
        ctx.Transactions.Add(tx);

        await ctx.SaveChangesAsync(ct);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoard(player.Id, dto)
        );
    }

    // ============================================================
    // GetBoardsForGame / GetBoardsForPlayer
    // ============================================================

    [Fact]
    public async Task GetBoardsForGame_Returns_OnlyNonDeletedBoards_ForGivenGame_OrderedByCreatedAt()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: create a small, controlled dataset (без сидера)
        var player     = CreateActivePlayer("boards-game@test.local");
        var targetGame = CreateGame(2025, 1, isActive: true);
        var otherGame  = CreateGame(2025, 2, isActive: false); // щоб не ламати idx_game_single_active

        ctx.Players.Add(player);
        ctx.Games.AddRange(targetGame, otherGame);
        await ctx.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;

        // Two boards for the target game (one deleted, one not)
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
            Deletedat    = now.AddMinutes(3) // soft-deleted
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

        // Board for another game – must NOT be returned
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

        // Act
        var result = await boardService.GetBoardsForGame(targetGame.Id);

        // Assert: only non-deleted boards for the target game
        Assert.Equal(2, result.Count);
        Assert.All(result, b =>
        {
            Assert.Equal(targetGame.Id, b.Gameid);
            Assert.Null(b.Deletedat);
        });

        // Assert: ordered by CreatedAt ascending (old first, new second)
        Assert.Equal(
            new[] { boardOld.Id, boardNew.Id },
            result.Select(b => b.Id).ToArray()
        );

        // Assert: navigation properties are loaded (because of Include)
        Assert.All(result, b => Assert.NotNull(b.Player));
        Assert.All(result, b => Assert.NotNull(b.Game));
    }

    [Fact]
    public async Task GetBoardsForPlayer_Returns_OnlyNonDeletedBoards_ForGivenPlayer_OrderedNewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;

        var player      = CreateActivePlayer("boards-player@test.local");
        var otherPlayer = CreateActivePlayer("other-player@test.local");
        var game        = CreateGame(2025, 1, isActive: true);

        ctx.Players.AddRange(player, otherPlayer);
        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);

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
            Createdat    = DateTime.UtcNow.AddMinutes(-10),
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
            Createdat    = DateTime.UtcNow.AddMinutes(-5),
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
            Createdat    = DateTime.UtcNow.AddMinutes(-3),
            Deletedat    = DateTime.UtcNow
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
            Createdat    = DateTime.UtcNow.AddMinutes(-1),
            Deletedat    = null
        };

        ctx.Boards.AddRange(olderBoard, newerBoard, deletedBoard, otherPlayerBoard);
        await ctx.SaveChangesAsync(ct);

        // Act
        var result = await boardService.GetBoardsForPlayer(player.Id);

        // Assert: only boards for this player and not soft-deleted
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(player.Id, b.Playerid));
        Assert.All(result, b => Assert.Null(b.Deletedat));

        // Order by CreatedAt descending (newest first)
        Assert.Equal(
            new[] { newerBoard.Id, olderBoard.Id },
            result.Select(b => b.Id).ToArray()
        );
    }

    // ============================================================
    // CreateBoardForCurrentUser / GetBoardsForCurrentUser
    // ============================================================

    [Fact]
    public async Task CreateBoardForCurrentUser_ResolvesPlayerFromEmailClaim_AndCreatesBoard()
    {
        var ct = TestContext.Current.CancellationToken;

        const string email = "claims-player@test.local";

        var player = CreateActivePlayer(email);
        var game   = CreateGame(2025, 4, isActive: true);

        ctx.Players.Add(player);
        ctx.Games.Add(game);

        // Give enough balance
        var tx = CreateApprovedTransaction(player.Id, amount: 100);
        ctx.Transactions.Add(tx);

        await ctx.SaveChangesAsync(ct);

        // ClaimsPrincipal with email
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var dto = new CreateBoardRequestDto
        {
            GameId      = game.Id,
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        // Act
        var board = await boardService.CreateBoardForCurrentUser(user, dto);

        // Assert: board belongs to the resolved player
        Assert.Equal(player.Id, board.Playerid);
        Assert.Equal(game.Id, board.Gameid);
    }

    [Fact]
    public async Task CreateBoardForCurrentUser_Throws_When_EmailClaimMissing()
    {
        // Arrange: empty identity without email claim
        var identity = new ClaimsIdentity();
        var user     = new ClaimsPrincipal(identity);

        var dto = new CreateBoardRequestDto
        {
            GameId      = "dummy",
            Numbers     = new[] { 1, 2, 3, 4, 5 },
            RepeatWeeks = 0
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await boardService.CreateBoardForCurrentUser(user, dto)
        );
    }

    [Fact]
    public async Task GetBoardsForCurrentUser_Returns_BoardsForResolvedPlayer()
    {
        var ct = TestContext.Current.CancellationToken;

        const string email = "history-claims@test.local";

        var player      = CreateActivePlayer(email);
        var otherPlayer = CreateActivePlayer("other-claims@test.local");
        var game        = CreateGame(2025, 5, isActive: true);

        ctx.Players.AddRange(player, otherPlayer);
        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);

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
            Createdat    = DateTime.UtcNow.AddMinutes(-5),
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
            Createdat    = DateTime.UtcNow.AddMinutes(-1),
            Deletedat    = null
        };

        ctx.Boards.AddRange(boardForPlayer, boardForOther);
        await ctx.SaveChangesAsync(ct);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = await boardService.GetBoardsForCurrentUser(user);

        // Assert: only boards for the resolved player
        Assert.Single(result);
        Assert.Equal(player.Id, result[0].Playerid);
        Assert.Equal(boardForPlayer.Id, result[0].Id);
    }
}
