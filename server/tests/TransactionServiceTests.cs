// server/tests/TransactionServiceTests.cs

using System.Security.Claims;
using api.Models.Responses;
using api.Models.Transactions;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
// All validation in TransactionService uses DataAnnotations.ValidationException
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;


namespace tests;

/// <summary>
/// Service-level tests for <see cref="TransactionService"/>:
/// - creating transactions (admin + current user)
/// - approving / rejecting
/// - pending + history queries
/// - balance calculation for players
/// Uses TestTransactionScope so each test runs in isolation.
/// </summary>
public class TransactionServiceTests(
    ITransactionService transactionService,
    MyDbContext ctx,
    TestTransactionScope transactionScope,
    TimeProvider timeProvider,
    ITestOutputHelper outputHelper) : IAsyncLifetime
{
    // ---------------------------------------------------------------------
    // xUnit v3 lifecycle
    // ---------------------------------------------------------------------

    /// <summary>
    /// Start a new database transaction before each test.
    /// All changes are rolled back by <see cref="TestTransactionScope"/>.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await transactionScope.BeginTransactionAsync(ct);
    }

    /// <summary>
    /// No extra async cleanup needed.
    /// Transaction rollback happens inside TestTransactionScope.Dispose().
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ---------------------------------------------------------------------
    // Helper methods
    // ---------------------------------------------------------------------

    /// <summary>
    /// Helper to create a unique player with defaults.
    /// Uses TimeProvider for deterministic timestamps.
    /// </summary>
    private async Task<Player> CreateUniquePlayer(string emailPrefix)
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var uniqueId = Guid.NewGuid().ToString();

        var player = new Player
        {
            Id          = uniqueId,
            Fullname    = "Test Player",
            Email       = $"{emailPrefix}-{uniqueId}@test.local",
            Phone       = "12345678",
            Isactive    = true,
            Activatedat = now,
            Createdat   = now,
            Deletedat   = null
        };

        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);
        return player;
    }

    /// <summary>
    /// Helper to create a unique game (used when boards are needed for balance tests).
    /// </summary>
    private async Task<Game> CreateUniqueGame()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var random = new Random();

        var game = new Game
        {
            Id         = Guid.NewGuid().ToString(),
            Year       = now.Year + random.Next(0, 10),
            Weeknumber = random.Next(1, 52),
            Isactive   = false,
            Createdat  = now,
            Closedat   = null,
            Deletedat  = null
        };

        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);
        return game;
    }

    /// <summary>
    /// Helper to generate a reasonably unique MobilePay number for tests.
    /// </summary>
    private static string NewMobilePayNumber()
    {
        var ticks  = DateTime.UtcNow.Ticks.ToString();
        var random = Random.Shared.Next(1000, 9999).ToString();
        return ticks + random;
    }

    // ============================================================
    // CreateTransaction (admin)
    // ============================================================

    [Fact]
    public async Task CreateTransaction_Succeeds_ForExistingPlayer_AndUniqueMobilePayNumber()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var player = await CreateUniquePlayer("tx-admin-happy");

        var dto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = player.Id,
            MobilePayNumber = NewMobilePayNumber(),
            Amount          = 150
        };

        // Act
        var tx = await transactionService.CreateTransaction(dto);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tx.Id));
        Assert.Equal(player.Id, tx.Playerid);
        Assert.Equal(dto.MobilePayNumber, tx.Mobilepaynumber);
        Assert.Equal(dto.Amount, tx.Amount);
        Assert.Equal("Pending", tx.Status);
        Assert.Null(tx.Approvedat);
        Assert.Null(tx.Deletedat);
        Assert.Null(tx.Rejectionreason);

        var fromDb = await ctx.Transactions.SingleAsync(t => t.Id == tx.Id, ct);
        Assert.Equal(tx.Playerid, fromDb.Playerid);
        Assert.Equal(tx.Status, fromDb.Status);
        Assert.Equal(tx.Amount, fromDb.Amount);

        outputHelper.WriteLine($"[Transaction] Created {tx.Id} with amount {tx.Amount} for player {player.Id}");
    }

    [Fact]
    public async Task CreateTransaction_Throws_When_PlayerDoesNotExist()
    {
        // Arrange
        var dto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = Guid.NewGuid().ToString(),
            MobilePayNumber = NewMobilePayNumber(),
            Amount          = 100
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransaction(dto));
    }

    [Fact]
    public async Task CreateTransaction_Throws_When_MobilePayNumberAlreadyExists()
    {
        // Arrange
        var player = await CreateUniquePlayer("tx-duplicate");
        var mobile = NewMobilePayNumber();

        var firstDto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = player.Id,
            MobilePayNumber = mobile,
            Amount          = 50
        };

        var first = await transactionService.CreateTransaction(firstDto);
        outputHelper.WriteLine($"[Transaction] First tx created with MobilePayNumber {mobile}, id={first.Id}");

        var secondDto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = player.Id,
            MobilePayNumber = mobile,
            Amount          = 75
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransaction(secondDto));
    }

    [Fact]
    public async Task CreateTransaction_Throws_When_DtoFailsAttributeValidation()
    {
        // Arrange: violates Required / Range / MinLength
        var dto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = "",
            MobilePayNumber = "123",
            Amount          = 0
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransaction(dto));
    }

    // ============================================================
    // CreateTransactionForCurrentUser
    // ============================================================

    [Fact]
    public async Task CreateTransactionForCurrentUser_Succeeds_When_EmailClaimAndPlayerExist()
    {
        // Arrange
        var player = await CreateUniquePlayer("tx-current-user");

        var dto = new CreateTransactionForCurrentUserRequestDto
        {
            MobilePayNumber = NewMobilePayNumber(),
            Amount          = 200
        };

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, player.Email) },
            authenticationType: "TestAuth");

        var user = new ClaimsPrincipal(identity);

        // Act
        var tx = await transactionService.CreateTransactionForCurrentUser(user, dto);

        // Assert
        Assert.Equal(player.Id, tx.Playerid);
        Assert.Equal(dto.Amount, tx.Amount);
        Assert.Equal(dto.MobilePayNumber, tx.Mobilepaynumber);
        Assert.Equal("Pending", tx.Status);
    }

    [Fact]
    public async Task CreateTransactionForCurrentUser_Throws_When_EmailClaimMissing()
    {
        // Arrange
        var dto = new CreateTransactionForCurrentUserRequestDto
        {
            MobilePayNumber = NewMobilePayNumber(),
            Amount          = 100
        };

        var identity = new ClaimsIdentity();
        var user     = new ClaimsPrincipal(identity);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransactionForCurrentUser(user, dto));
    }

    [Fact]
    public async Task CreateTransactionForCurrentUser_Throws_When_PlayerNotFoundForEmail()
    {
        // Arrange
        var dto = new CreateTransactionForCurrentUserRequestDto
        {
            MobilePayNumber = NewMobilePayNumber(),
            Amount          = 120
        };

        const string email = "no-player-for-this-email@test.local";

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "TestAuth");

        var user = new ClaimsPrincipal(identity);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransactionForCurrentUser(user, dto));
    }

    // ============================================================
    // ApproveTransaction
    // ============================================================

    [Fact]
    public async Task ApproveTransaction_SetsStatusApproved_AndApprovedAt_ForPendingTransaction()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange: a pending transaction
        var player = await CreateUniquePlayer("approve");

        var tx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 300,
            Status          = "Pending",
            Createdat       = now.AddMinutes(-10),
            Approvedat      = null,
            Deletedat       = null,
            Rejectionreason = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        // Act
        var updated = await transactionService.ApproveTransaction(tx.Id);

        // Assert
        Assert.Equal("Approved", updated.Status);
        Assert.NotNull(updated.Approvedat);
        Assert.Null(updated.Rejectionreason);

        var fromDb = await ctx.Transactions.SingleAsync(t => t.Id == tx.Id, ct);
        Assert.Equal("Approved", fromDb.Status);
        Assert.NotNull(fromDb.Approvedat);
    }

    [Fact]
    public async Task ApproveTransaction_Throws_When_IdIsEmpty()
    {
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.ApproveTransaction(""));
    }

    [Fact]
    public async Task ApproveTransaction_Throws_When_TransactionNotFound()
    {
        var unknownId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.ApproveTransaction(unknownId));
    }

    [Fact]
    public async Task ApproveTransaction_Throws_When_StatusIsNotPending()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange: already approved transaction
        var player = await CreateUniquePlayer("approve-invalid-status");

        var tx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 200,
            Status          = "Approved",
            Createdat       = now,
            Approvedat      = now,
            Deletedat       = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.ApproveTransaction(tx.Id));
    }

    // ============================================================
    // RejectTransaction
    // ============================================================

    [Fact]
    public async Task RejectTransaction_SetsStatusRejected_ForPendingTransaction()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange
        var player = await CreateUniquePlayer("reject");

        var tx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 250,
            Status          = "Pending",
            Createdat       = now.AddMinutes(-3),
            Approvedat      = null,
            Deletedat       = null,
            Rejectionreason = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        // Act
        var updated = await transactionService.RejectTransaction(tx.Id);

        // Assert
        Assert.Equal("Rejected", updated.Status);
        Assert.Null(updated.Approvedat);

        var fromDb = await ctx.Transactions.SingleAsync(t => t.Id == tx.Id, ct);
        Assert.Equal("Rejected", fromDb.Status);
        Assert.Null(fromDb.Approvedat);
    }

    [Fact]
    public async Task RejectTransaction_Throws_When_IdIsEmpty()
    {
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.RejectTransaction(""));
    }

    [Fact]
    public async Task RejectTransaction_Throws_When_TransactionNotFound()
    {
        var unknownId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.RejectTransaction(unknownId));
    }

    [Fact]
    public async Task RejectTransaction_Throws_When_StatusIsNotPending()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange: already approved transaction
        var player = await CreateUniquePlayer("reject-invalid-status");

        var tx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 250,
            Status          = "Approved",
            Createdat       = now,
            Approvedat      = now,
            Deletedat       = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.RejectTransaction(tx.Id));
    }

    // ============================================================
    // GetTransactionsForPlayer
    // ============================================================

    [Fact]
    public async Task GetTransactionsForPlayer_Returns_OnlyNonDeleted_ForPlayer_OrderedNewestFirst()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange
        var player      = await CreateUniquePlayer("history-player");
        var otherPlayer = await CreateUniquePlayer("history-other");

        var older = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 100,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-10),
            Deletedat       = null
        };

        var newer = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 200,
            Status          = "Pending",
            Createdat       = now.AddMinutes(-5),
            Deletedat       = null
        };

        var deleted = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 300,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-2),
            Deletedat       = now
        };

        var other = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = otherPlayer.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 400,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-1),
            Deletedat       = null
        };

        ctx.Transactions.AddRange(older, newer, deleted, other);
        await ctx.SaveChangesAsync(ct);

        // Act
        var result = await transactionService.GetTransactionsForPlayer(player.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t =>
        {
            Assert.Equal(player.Id, t.Playerid);
            Assert.Null(t.Deletedat);
        });

        // Must be ordered by Createdat descending (newest first)
        Assert.Equal(new[] { newer.Id, older.Id }, result.Select(t => t.Id).ToArray());
        Assert.All(result, t => Assert.NotNull(t.Player));
    }

    // ============================================================
    // GetPendingTransactions
    // ============================================================

    [Fact]
    public async Task GetPendingTransactions_Returns_PendingOnly_OrderedByCreatedAt_AndIncludesPlayer()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange
        var p1 = await CreateUniquePlayer("pending-1");
        var p2 = await CreateUniquePlayer("pending-2");

        var pendingOld = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = p1.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 50,
            Status          = "Pending",
            Createdat       = now.AddMinutes(-10),
            Deletedat       = null
        };

        var pendingNew = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = p2.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 60,
            Status          = "Pending",
            Createdat       = now.AddMinutes(-5),
            Deletedat       = null
        };

        var approved = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = p1.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 70,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-3),
            Deletedat       = null
        };

        var deletedPending = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = p1.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 80,
            Status          = "Pending",
            Createdat       = now.AddMinutes(-1),
            Deletedat       = now
        };

        ctx.Transactions.AddRange(pendingOld, pendingNew, approved, deletedPending);
        await ctx.SaveChangesAsync(ct);

        // Act
        var result = await transactionService.GetPendingTransactions();

        // Assert: only non-deleted pending transactions, ordered by Createdat ascending
        var ours = result
            .Where(t => t.Id == pendingOld.Id || t.Id == pendingNew.Id)
            .OrderBy(t => t.Createdat)
            .ToList();

        Assert.Equal(2, ours.Count);
        Assert.All(ours, t => Assert.Equal("Pending", t.Status));
        Assert.Equal(new[] { pendingOld.Id, pendingNew.Id }, ours.Select(t => t.Id).ToArray());
        Assert.All(ours, t => Assert.NotNull(t.Player));
    }

    // ============================================================
    // GetTransactionsHistory
    // ============================================================

    [Fact]
    public async Task GetTransactionsHistory_Filters_ByPlayer_AndExcludesPendingByDefault()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange
        var player = await CreateUniquePlayer("history-filter");

        var pending = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 10,
            Status          = "Pending",
            Createdat       = now.AddMinutes(-15),
            Deletedat       = null
        };

        var approved = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 20,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-10),
            Deletedat       = null
        };

        var rejected = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 30,
            Status          = "Rejected",
            Createdat       = now.AddMinutes(-5),
            Deletedat       = null
        };

        ctx.Transactions.AddRange(pending, approved, rejected);
        await ctx.SaveChangesAsync(ct);

        // Act: status == null => exclude Pending by default
        var history = await transactionService.GetTransactionsHistory(playerId: player.Id, status: null);

        // Assert
        Assert.Equal(2, history.Count);
        Assert.All(history, t =>
        {
            Assert.Equal(player.Id, t.Playerid);
            Assert.NotEqual("Pending", t.Status);
        });

        // Newest first (Rejected, then Approved)
        Assert.Equal(new[] { rejected.Id, approved.Id }, history.Select(t => t.Id).ToArray());
    }

    [Fact]
    public async Task GetTransactionsHistory_Filters_ByStatus_WhenProvided()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange
        var player = await CreateUniquePlayer("history-status");

        var approved = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 20,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-10),
            Deletedat       = null
        };

        var rejected = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 30,
            Status          = "Rejected",
            Createdat       = now.AddMinutes(-5),
            Deletedat       = null
        };

        ctx.Transactions.AddRange(approved, rejected);
        await ctx.SaveChangesAsync(ct);

        // Act
        var onlyApproved = await transactionService.GetTransactionsHistory(player.Id, status: "Approved");

        // Assert
        Assert.Single(onlyApproved);
        Assert.Equal("Approved", onlyApproved[0].Status);
        Assert.Equal(approved.Id, onlyApproved[0].Id);
    }

    [Fact]
    public async Task GetTransactionsHistory_Throws_When_StatusIsInvalid()
    {
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetTransactionsHistory(playerId: null, status: "SomethingElse"));
    }

    // ============================================================
    // GetPlayerBalance / GetBalanceForCurrentUser / GetTransactionsForCurrentUser
    // ============================================================

    [Fact]
    public async Task GetPlayerBalance_Computes_ApprovedMinusBoardsPrice()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange
        var player = await CreateUniquePlayer("balance");
        var game   = await CreateUniqueGame();

        var board1 = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 20,
            Iswinning    = false,
            Repeatweeks  = 0,
            Repeatactive = false,
            Createdat    = now.AddMinutes(-20),
            Deletedat    = null
        };

        var board2 = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 6, 7, 8, 9, 10 }.ToList(),
            Price        = 40,
            Iswinning    = false,
            Repeatweeks  = 0,
            Repeatactive = false,
            Createdat    = now.AddMinutes(-10),
            Deletedat    = null
        };

        var deletedBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 100,
            Iswinning    = false,
            Repeatweeks  = 0,
            Repeatactive = false,
            Createdat    = now.AddMinutes(-5),
            Deletedat    = now
        };

        ctx.Boards.AddRange(board1, board2, deletedBoard);

        var approved1 = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 100,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-30),
            Deletedat       = null
        };

        var approved2 = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 50,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-25),
            Deletedat       = null
        };

        var pending = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 999,
            Status          = "Pending",
            Createdat       = now.AddMinutes(-15),
            Deletedat       = null
        };

        var rejected = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 888,
            Status          = "Rejected",
            Createdat       = now.AddMinutes(-12),
            Deletedat       = null
        };

        ctx.Transactions.AddRange(approved1, approved2, pending, rejected);
        await ctx.SaveChangesAsync(ct);

        // Act
        PlayerBalanceResponseDto balance = await transactionService.GetPlayerBalance(player.Id);

        // Assert: (100 + 50) - (20 + 40) = 90
        Assert.Equal(player.Id, balance.PlayerId);
        Assert.Equal(90, balance.Balance);
    }

    [Fact]
    public async Task GetPlayerBalance_Throws_When_PlayerNotFound()
    {
        var unknownPlayerId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetPlayerBalance(unknownPlayerId));
    }

    [Fact]
    public async Task GetTransactionsForCurrentUser_Returns_OnlyNonDeletedTransactions_ForResolvedPlayer()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange
        var player      = await CreateUniquePlayer("tx-current-history");
        var otherPlayer = await CreateUniquePlayer("tx-current-other");

        var tx1 = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 10,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-10),
            Deletedat       = null
        };

        var tx2 = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 20,
            Status          = "Pending",
            Createdat       = now.AddMinutes(-5),
            Deletedat       = null
        };

        var deleted = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 30,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-3),
            Deletedat       = now
        };

        var otherTx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = otherPlayer.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 40,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-1),
            Deletedat       = null
        };

        ctx.Transactions.AddRange(tx1, tx2, deleted, otherTx);
        await ctx.SaveChangesAsync(ct);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, player.Email) },
            authenticationType: "TestAuth");

        var user = new ClaimsPrincipal(identity);

        // Act
        var result = await transactionService.GetTransactionsForCurrentUser(user);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t =>
        {
            Assert.Equal(player.Id, t.Playerid);
            Assert.Null(t.Deletedat);
        });
    }

    [Fact]
    public async Task GetBalanceForCurrentUser_ResolvesPlayerAndReturnsSameBalanceAsGetPlayerBalance()
    {
        var ct  = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Arrange
        var player = await CreateUniquePlayer("balance-current");
        var game   = await CreateUniqueGame();

        var board = new Board
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

        var approvedTx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 50,
            Status          = "Approved",
            Createdat       = now.AddMinutes(-15),
            Deletedat       = null
        };

        ctx.Boards.Add(board);
        ctx.Transactions.Add(approvedTx);
        await ctx.SaveChangesAsync(ct);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, player.Email) },
            authenticationType: "TestAuth");

        var user = new ClaimsPrincipal(identity);

        // Act
        var balanceDirect     = await transactionService.GetPlayerBalance(player.Id);
        var balanceFromClaims = await transactionService.GetBalanceForCurrentUser(user);

        // Assert: 50 - 20 = 30
        Assert.Equal(30, balanceDirect.Balance);
        Assert.Equal(30, balanceFromClaims.Balance);
        Assert.Equal(balanceDirect.Balance, balanceFromClaims.Balance);
    }

    [Fact]
    public async Task GetBalanceForCurrentUser_Throws_When_EmailMissing()
    {
        var identity = new ClaimsIdentity();
        var user     = new ClaimsPrincipal(identity);

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetBalanceForCurrentUser(user));
    }

    [Fact]
    public async Task GetBalanceForCurrentUser_Throws_When_PlayerNotFoundForEmail()
    {
        const string email = "missing-player-balance@test.local";

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "TestAuth");

        var user = new ClaimsPrincipal(identity);

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetBalanceForCurrentUser(user));
    }
}
