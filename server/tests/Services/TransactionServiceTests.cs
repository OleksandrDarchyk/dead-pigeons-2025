// server/tests/TransactionServiceTests.cs
using System.Security.Claims;
using api.Models.Responses;
using api.Models.Transactions;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace tests.Services;

/// <summary>
/// Service-level tests for <see cref="TransactionService"/>:
/// - create transactions (admin + current user)
/// - approve / reject
/// - pending + history queries
/// - balance calculation
/// Uses TestTransactionScope so each test runs in isolation (rollback after each test).
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

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await transactionScope.BeginTransactionAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        // Ensure rollback even if DI disposal order changes.
        transactionScope.Dispose();
        return ValueTask.CompletedTask;
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private async Task<Player> CreateUniquePlayer(string emailPrefix)
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var id = Guid.NewGuid().ToString();

        var player = new Player
        {
            Id = id,
            Fullname = "Test Player",
            Email = $"{emailPrefix}-{id}@test.local",
            Phone = "12345678",
            Isactive = true,
            Activatedat = now,
            Createdat = now,
            Deletedat = null
        };

        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);
        return player;
    }

    private async Task<Game> CreateUniqueGame()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Use far-future year to avoid collisions with any seeded/realistic data.
        var week = (Math.Abs(Guid.NewGuid().GetHashCode()) % 52) + 1;

        var game = new Game
        {
            Id = Guid.NewGuid().ToString(),
            Year = 2400,
            Weeknumber = week,
            Isactive = false,
            Createdat = now,
            Closedat = null,
            Deletedat = null
        };

        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);
        return game;
    }

    private static string NewMobilePayNumber() => Guid.NewGuid().ToString("N");

    private static ClaimsPrincipal UserWithEmail(string email)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    // ============================================================
    // CreateTransaction (admin)
    // ============================================================

    [Fact]
    public async Task CreateTransaction_Succeeds_ForExistingPlayer_AndUniqueMobilePayNumber()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = await CreateUniquePlayer("tx-admin-happy");

        var dto = new AdminCreateTransactionRequestDto
        {
            PlayerId = player.Id,
            MobilePayNumber = NewMobilePayNumber(),
            Amount = 150
        };

        var tx = await transactionService.CreateTransaction(dto);

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

        outputHelper.WriteLine($"[Transaction] Created {tx.Id} amount={tx.Amount} player={player.Id}");
    }

    [Fact]
    public async Task CreateTransaction_Throws_When_PlayerDoesNotExist()
    {
        var dto = new AdminCreateTransactionRequestDto
        {
            PlayerId = Guid.NewGuid().ToString(),
            MobilePayNumber = NewMobilePayNumber(),
            Amount = 100
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransaction(dto));
    }

    [Fact]
    public async Task CreateTransaction_Throws_When_MobilePayNumberAlreadyExists()
    {
        var player = await CreateUniquePlayer("tx-duplicate");
        var mobile = NewMobilePayNumber();

        var first = await transactionService.CreateTransaction(new AdminCreateTransactionRequestDto
        {
            PlayerId = player.Id,
            MobilePayNumber = mobile,
            Amount = 50
        });

        outputHelper.WriteLine($"[Transaction] First tx id={first.Id} mobile={mobile}");

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransaction(new AdminCreateTransactionRequestDto
            {
                PlayerId = player.Id,
                MobilePayNumber = mobile,
                Amount = 75
            }));
    }

    [Fact]
    public async Task CreateTransaction_Throws_When_DtoFailsAttributeValidation()
    {
        var dto = new AdminCreateTransactionRequestDto
        {
            PlayerId = "",
            MobilePayNumber = "123",
            Amount = 0
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransaction(dto));
    }

    // ============================================================
    // CreateTransactionForCurrentUser
    // ============================================================

    [Fact]
    public async Task CreateTransactionForCurrentUser_Succeeds_When_EmailClaimAndPlayerExist()
    {
        var player = await CreateUniquePlayer("tx-current-user");

        var dto = new CreateTransactionForCurrentUserRequestDto
        {
            MobilePayNumber = NewMobilePayNumber(),
            Amount = 200
        };

        var user = UserWithEmail(player.Email);

        var tx = await transactionService.CreateTransactionForCurrentUser(user, dto);

        Assert.Equal(player.Id, tx.Playerid);
        Assert.Equal(dto.Amount, tx.Amount);
        Assert.Equal(dto.MobilePayNumber, tx.Mobilepaynumber);
        Assert.Equal("Pending", tx.Status);
    }

    [Fact]
    public async Task CreateTransactionForCurrentUser_Throws_When_EmailClaimMissing()
    {
        var dto = new CreateTransactionForCurrentUserRequestDto
        {
            MobilePayNumber = NewMobilePayNumber(),
            Amount = 100
        };

        var user = new ClaimsPrincipal(new ClaimsIdentity()); // no email claim

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransactionForCurrentUser(user, dto));
    }

    [Fact]
    public async Task CreateTransactionForCurrentUser_Throws_When_PlayerNotFoundForEmail()
    {
        var dto = new CreateTransactionForCurrentUserRequestDto
        {
            MobilePayNumber = NewMobilePayNumber(),
            Amount = 120
        };

        var user = UserWithEmail("no-player-for-this-email@test.local");

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransactionForCurrentUser(user, dto));
    }

    // ============================================================
    // ApproveTransaction
    // ============================================================

    [Fact]
    public async Task ApproveTransaction_SetsStatusApproved_AndApprovedAt_ForPendingTransaction()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("approve");

        var tx = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 300,
            Status = "Pending",
            Createdat = now.AddMinutes(-10),
            Approvedat = null,
            Deletedat = null,
            Rejectionreason = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        var updated = await transactionService.ApproveTransaction(tx.Id);

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
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.ApproveTransaction(Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task ApproveTransaction_Throws_When_StatusIsNotPending()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("approve-invalid-status");

        var tx = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 200,
            Status = "Approved",
            Createdat = now,
            Approvedat = now,
            Deletedat = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.ApproveTransaction(tx.Id));
    }

    // ============================================================
    // RejectTransaction
    // ============================================================

    [Fact]
    public async Task RejectTransaction_SetsStatusRejected_ForPendingTransaction()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("reject");

        var tx = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 250,
            Status = "Pending",
            Createdat = now.AddMinutes(-3),
            Approvedat = null,
            Deletedat = null,
            Rejectionreason = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        var updated = await transactionService.RejectTransaction(tx.Id);

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
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.RejectTransaction(Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task RejectTransaction_Throws_When_StatusIsNotPending()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("reject-invalid-status");

        var tx = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 250,
            Status = "Approved",
            Createdat = now,
            Approvedat = now,
            Deletedat = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.RejectTransaction(tx.Id));
    }

    // ============================================================
    // GetTransactionsForPlayer
    // ============================================================

    [Fact]
    public async Task GetTransactionsForPlayer_Returns_OnlyNonDeleted_ForPlayer_OrderedNewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("history-player");
        var otherPlayer = await CreateUniquePlayer("history-other");

        var older = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 100,
            Status = "Approved",
            Createdat = now.AddMinutes(-10),
            Deletedat = null
        };

        var newer = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 200,
            Status = "Pending",
            Createdat = now.AddMinutes(-5),
            Deletedat = null
        };

        var deleted = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 300,
            Status = "Approved",
            Createdat = now.AddMinutes(-2),
            Deletedat = now
        };

        var other = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = otherPlayer.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 400,
            Status = "Approved",
            Createdat = now.AddMinutes(-1),
            Deletedat = null
        };

        ctx.Transactions.AddRange(older, newer, deleted, other);
        await ctx.SaveChangesAsync(ct);

        var result = await transactionService.GetTransactionsForPlayer(player.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, t =>
        {
            Assert.Equal(player.Id, t.Playerid);
            Assert.Null(t.Deletedat);
            Assert.NotNull(t.Player);
        });

        Assert.Equal(new[] { newer.Id, older.Id }, result.Select(t => t.Id).ToArray());
    }

    [Fact]
    public async Task GetTransactionsForPlayer_Returns_EmptyList_When_PlayerHasNoTransactions()
    {
        var player = await CreateUniquePlayer("history-empty");

        var result = await transactionService.GetTransactionsForPlayer(player.Id);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ============================================================
    // GetPendingTransactions
    // ============================================================

    [Fact]
    public async Task GetPendingTransactions_Returns_PendingOnly_OrderedByCreatedAt_AndIncludesPlayer()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var p1 = await CreateUniquePlayer("pending-1");
        var p2 = await CreateUniquePlayer("pending-2");

        var pendingOld = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = p1.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 50,
            Status = "Pending",
            Createdat = now.AddMinutes(-10),
            Deletedat = null
        };

        var pendingNew = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = p2.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 60,
            Status = "Pending",
            Createdat = now.AddMinutes(-5),
            Deletedat = null
        };

        var approved = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = p1.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 70,
            Status = "Approved",
            Createdat = now.AddMinutes(-3),
            Deletedat = null
        };

        var deletedPending = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = p1.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 80,
            Status = "Pending",
            Createdat = now.AddMinutes(-1),
            Deletedat = now
        };

        ctx.Transactions.AddRange(pendingOld, pendingNew, approved, deletedPending);
        await ctx.SaveChangesAsync(ct);

        var result = await transactionService.GetPendingTransactions();

        // Should be exactly our 2 pending non-deleted, ordered oldest -> newest
        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { pendingOld.Id, pendingNew.Id }, result.Select(t => t.Id).ToArray());
        Assert.All(result, t =>
        {
            Assert.Equal("Pending", t.Status);
            Assert.Null(t.Deletedat);
            Assert.NotNull(t.Player);
        });
    }

    [Fact]
    public async Task GetPendingTransactions_Returns_EmptyList_When_NoPendingExists()
    {
        var result = await transactionService.GetPendingTransactions();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ============================================================
    // GetTransactionsHistory
    // ============================================================

    [Fact]
    public async Task GetTransactionsHistory_Filters_ByPlayer_AndExcludesPendingByDefault()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("history-filter");

        var pending = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 10,
            Status = "Pending",
            Createdat = now.AddMinutes(-15),
            Deletedat = null
        };

        var approved = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 20,
            Status = "Approved",
            Createdat = now.AddMinutes(-10),
            Deletedat = null
        };

        var rejected = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 30,
            Status = "Rejected",
            Createdat = now.AddMinutes(-5),
            Deletedat = null
        };

        ctx.Transactions.AddRange(pending, approved, rejected);
        await ctx.SaveChangesAsync(ct);

        var history = await transactionService.GetTransactionsHistory(playerId: player.Id, status: null);

        Assert.Equal(2, history.Count);
        Assert.All(history, t =>
        {
            Assert.Equal(player.Id, t.Playerid);
            Assert.NotEqual("Pending", t.Status);
            Assert.NotNull(t.Player);
        });

        Assert.Equal(new[] { rejected.Id, approved.Id }, history.Select(t => t.Id).ToArray());
    }

    [Fact]
    public async Task GetTransactionsHistory_Filters_ByStatus_WhenProvided()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("history-status");

        var approved = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 20,
            Status = "Approved",
            Createdat = now.AddMinutes(-10),
            Deletedat = null
        };

        var rejected = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 30,
            Status = "Rejected",
            Createdat = now.AddMinutes(-5),
            Deletedat = null
        };

        ctx.Transactions.AddRange(approved, rejected);
        await ctx.SaveChangesAsync(ct);

        var onlyApproved = await transactionService.GetTransactionsHistory(player.Id, status: "Approved");

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

    [Fact]
    public async Task GetTransactionsHistory_Returns_EmptyList_When_NoApprovedOrRejectedExists()
    {
        var player = await CreateUniquePlayer("history-empty");

        var history = await transactionService.GetTransactionsHistory(playerId: player.Id, status: null);

        Assert.NotNull(history);
        Assert.Empty(history);
    }

    // ============================================================
    // GetPlayerBalance / Current user methods
    // ============================================================

    [Fact]
    public async Task GetPlayerBalance_Computes_ApprovedMinusBoardsPrice()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("balance");
        var game = await CreateUniqueGame();

        var board1 = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Gameid = game.Id,
            Numbers = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price = 20,
            Iswinning = false,
            Repeatweeks = 0,
            Repeatactive = false,
            Createdat = now.AddMinutes(-20),
            Deletedat = null
        };

        var board2 = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Gameid = game.Id,
            Numbers = new[] { 6, 7, 8, 9, 10 }.ToList(),
            Price = 40,
            Iswinning = false,
            Repeatweeks = 0,
            Repeatactive = false,
            Createdat = now.AddMinutes(-10),
            Deletedat = null
        };

        var deletedBoard = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Gameid = game.Id,
            Numbers = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price = 100,
            Iswinning = false,
            Repeatweeks = 0,
            Repeatactive = false,
            Createdat = now.AddMinutes(-5),
            Deletedat = now
        };

        ctx.Boards.AddRange(board1, board2, deletedBoard);

        var approved1 = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 100,
            Status = "Approved",
            Createdat = now.AddMinutes(-30),
            Deletedat = null
        };

        var approved2 = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 50,
            Status = "Approved",
            Createdat = now.AddMinutes(-25),
            Deletedat = null
        };

        var pending = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 999,
            Status = "Pending",
            Createdat = now.AddMinutes(-15),
            Deletedat = null
        };

        var rejected = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 888,
            Status = "Rejected",
            Createdat = now.AddMinutes(-12),
            Deletedat = null
        };

        ctx.Transactions.AddRange(approved1, approved2, pending, rejected);
        await ctx.SaveChangesAsync(ct);

        PlayerBalanceResponseDto balance = await transactionService.GetPlayerBalance(player.Id);

        Assert.Equal(player.Id, balance.PlayerId);
        Assert.Equal(90, balance.Balance); // (100 + 50) - (20 + 40) = 90
    }

    [Fact]
    public async Task GetPlayerBalance_Throws_When_PlayerNotFound()
    {
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetPlayerBalance(Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task GetTransactionsForCurrentUser_Returns_OnlyNonDeletedTransactions_ForResolvedPlayer()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("tx-current-history");
        var otherPlayer = await CreateUniquePlayer("tx-current-other");

        var tx1 = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 10,
            Status = "Approved",
            Createdat = now.AddMinutes(-10),
            Deletedat = null
        };

        var tx2 = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 20,
            Status = "Pending",
            Createdat = now.AddMinutes(-5),
            Deletedat = null
        };

        var deleted = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 30,
            Status = "Approved",
            Createdat = now.AddMinutes(-3),
            Deletedat = now
        };

        var otherTx = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = otherPlayer.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 40,
            Status = "Approved",
            Createdat = now.AddMinutes(-1),
            Deletedat = null
        };

        ctx.Transactions.AddRange(tx1, tx2, deleted, otherTx);
        await ctx.SaveChangesAsync(ct);

        var user = UserWithEmail(player.Email);

        var result = await transactionService.GetTransactionsForCurrentUser(user);

        Assert.Equal(2, result.Count);
        Assert.All(result, t =>
        {
            Assert.Equal(player.Id, t.Playerid);
            Assert.Null(t.Deletedat);
        });
    }

    [Fact]
    public async Task GetTransactionsForCurrentUser_Throws_When_EmailMissing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetTransactionsForCurrentUser(user));
    }

    [Fact]
    public async Task GetTransactionsForCurrentUser_Throws_When_PlayerNotFoundForEmail()
    {
        var user = UserWithEmail("missing-player-transactions@test.local");

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetTransactionsForCurrentUser(user));
    }

    [Fact]
    public async Task GetBalanceForCurrentUser_ResolvesPlayerAndReturnsSameBalanceAsGetPlayerBalance()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = await CreateUniquePlayer("balance-current");
        var game = await CreateUniqueGame();

        var board = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Gameid = game.Id,
            Numbers = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price = 20,
            Iswinning = false,
            Repeatweeks = 0,
            Repeatactive = false,
            Createdat = now.AddMinutes(-10),
            Deletedat = null
        };

        var approvedTx = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount = 50,
            Status = "Approved",
            Createdat = now.AddMinutes(-15),
            Deletedat = null
        };

        ctx.Boards.Add(board);
        ctx.Transactions.Add(approvedTx);
        await ctx.SaveChangesAsync(ct);

        var user = UserWithEmail(player.Email);

        var balanceDirect = await transactionService.GetPlayerBalance(player.Id);
        var balanceFromClaims = await transactionService.GetBalanceForCurrentUser(user);

        Assert.Equal(30, balanceDirect.Balance);     // 50 - 20
        Assert.Equal(30, balanceFromClaims.Balance); // 50 - 20
    }

    [Fact]
    public async Task GetBalanceForCurrentUser_Throws_When_EmailMissing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetBalanceForCurrentUser(user));
    }

    [Fact]
    public async Task GetBalanceForCurrentUser_Throws_When_PlayerNotFoundForEmail()
    {
        var user = UserWithEmail("missing-player-balance@test.local");

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetBalanceForCurrentUser(user));
    }
}
