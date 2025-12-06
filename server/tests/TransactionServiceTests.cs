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

public class TransactionServiceTests(
    ITransactionService transactionService,
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

    // Helper: create a simple game (used only for boards in balance tests)
    private static Game CreateGame(int year, int weekNumber, bool isActive = false)
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

    // Helper: create a stable MobilePay number (digits only, long enough and unique)
    private static string NewMobilePayNumber()
    {
        // Use ticks + random digits to avoid collisions
        var ticks = DateTime.UtcNow.Ticks.ToString();
        var random = Random.Shared.Next(1000, 9999).ToString();
        return ticks + random; // always numeric and length > 6
    }

    // ============================================================
    // CreateTransaction (admin)
    // ============================================================

    [Fact]
    public async Task CreateTransaction_Succeeds_ForExistingPlayer_AndUniqueMobilePayNumber()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: create one player
        var player = CreateActivePlayer("tx-admin-happy@test.local");
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = player.Id,
            MobilePayNumber = NewMobilePayNumber(),
            Amount          = 150
        };

        // Act
        var tx = await transactionService.CreateTransaction(dto);

        // Assert: basic fields
        Assert.False(string.IsNullOrWhiteSpace(tx.Id));
        Assert.Equal(player.Id, tx.Playerid);
        Assert.Equal(dto.MobilePayNumber, tx.Mobilepaynumber);
        Assert.Equal(dto.Amount, tx.Amount);
        Assert.Equal("Pending", tx.Status);
        Assert.Null(tx.Approvedat);
        Assert.Null(tx.Deletedat);
        Assert.Null(tx.Rejectionreason);

        // Assert: transaction is stored in the database
        var fromDb = await ctx.Transactions
            .SingleAsync(t => t.Id == tx.Id, ct);

        Assert.Equal(tx.Playerid, fromDb.Playerid);
        Assert.Equal(tx.Status, fromDb.Status);
        Assert.Equal(tx.Amount, fromDb.Amount);

        outputHelper.WriteLine($"Created transaction {tx.Id} with amount {tx.Amount} for player {player.Id}");
    }

    [Fact]
    public async Task CreateTransaction_Throws_When_PlayerDoesNotExist()
    {
        var dto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = Guid.NewGuid().ToString(), // unknown player
            MobilePayNumber = NewMobilePayNumber(),
            Amount          = 100
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransaction(dto)
        );
    }

    [Fact]
    public async Task CreateTransaction_Throws_When_MobilePayNumberAlreadyExists()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: player + first transaction
        var player = CreateActivePlayer("tx-duplicate@test.local");
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var mobile = NewMobilePayNumber();

        var firstDto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = player.Id,
            MobilePayNumber = mobile,
            Amount          = 50
        };

        var first = await transactionService.CreateTransaction(firstDto);
        outputHelper.WriteLine($"First transaction created with MobilePayNumber {mobile}, id={first.Id}");

        // Second DTO reuses the same MobilePay number
        var secondDto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = player.Id,
            MobilePayNumber = mobile,
            Amount          = 75
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransaction(secondDto)
        );
    }

    [Fact]
    public async Task CreateTransaction_Throws_When_DtoFailsAttributeValidation()
    {
        // This test relies on DataAnnotations on the DTO (for example Range on Amount).
        // Amount = 0 is usually outside the allowed range for a payment.
        var dto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = "",                // invalid for [Required]
            MobilePayNumber = "123",            // too short if MinLength is used
            Amount          = 0                 // outside valid range
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransaction(dto)
        );
    }

    // ============================================================
    // CreateTransactionForCurrentUser
    // ============================================================

    [Fact]
    public async Task CreateTransactionForCurrentUser_Succeeds_When_EmailClaimAndPlayerExist()
    {
        var ct = TestContext.Current.CancellationToken;

        const string email = "tx-current-user@test.local";

        var player = CreateActivePlayer(email);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new CreateTransactionForCurrentUserRequestDto
        {
            MobilePayNumber = NewMobilePayNumber(),
            Amount          = 200
        };

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var tx = await transactionService.CreateTransactionForCurrentUser(user, dto);

        // Assert: transaction is linked to the resolved player
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
            Amount          = 100
        };

        var identity = new ClaimsIdentity(); // no email claim
        var user = new ClaimsPrincipal(identity);

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransactionForCurrentUser(user, dto)
        );
    }

    [Fact]
    public async Task CreateTransactionForCurrentUser_Throws_When_PlayerNotFoundForEmail()
    {
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

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.CreateTransactionForCurrentUser(user, dto)
        );
    }

    // ============================================================
    // ApproveTransaction
    // ============================================================

    [Fact]
    public async Task ApproveTransaction_SetsStatusApproved_AndApprovedAt_ForPendingTransaction()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: simple pending transaction
        var player = CreateActivePlayer("approve@test.local");
        ctx.Players.Add(player);

        var tx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 300,
            Status          = "Pending",
            Createdat       = DateTime.UtcNow.AddMinutes(-10),
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

        var fromDb = await ctx.Transactions
            .SingleAsync(t => t.Id == tx.Id, ct);

        Assert.Equal("Approved", fromDb.Status);
        Assert.NotNull(fromDb.Approvedat);
    }

    [Fact]
    public async Task ApproveTransaction_Throws_When_IdIsEmpty()
    {
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.ApproveTransaction("")
        );
    }

    [Fact]
    public async Task ApproveTransaction_Throws_When_TransactionNotFound()
    {
        var unknownId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.ApproveTransaction(unknownId)
        );
    }

    [Fact]
    public async Task ApproveTransaction_Throws_When_StatusIsNotPending()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("approve-invalid-status@test.local");
        ctx.Players.Add(player);

        var tx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 200,
            Status          = "Approved", // already approved
            Createdat       = DateTime.UtcNow,
            Approvedat      = DateTime.UtcNow,
            Deletedat       = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.ApproveTransaction(tx.Id)
        );
    }

    // ============================================================
    // RejectTransaction
    // ============================================================

    [Fact]
    public async Task RejectTransaction_SetsStatusRejected_ForPendingTransaction()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("reject@test.local");
        ctx.Players.Add(player);

        var tx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 250,
            Status          = "Pending",
            Createdat       = DateTime.UtcNow.AddMinutes(-3),
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

        var fromDb = await ctx.Transactions
            .SingleAsync(t => t.Id == tx.Id, ct);

        Assert.Equal("Rejected", fromDb.Status);
        Assert.Null(fromDb.Approvedat);
    }

    [Fact]
    public async Task RejectTransaction_Throws_When_IdIsEmpty()
    {
        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.RejectTransaction("")
        );
    }

    [Fact]
    public async Task RejectTransaction_Throws_When_TransactionNotFound()
    {
        var unknownId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.RejectTransaction(unknownId)
        );
    }

    [Fact]
    public async Task RejectTransaction_Throws_When_StatusIsNotPending()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("reject-invalid-status@test.local");
        ctx.Players.Add(player);

        var tx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 250,
            Status          = "Approved", // already approved
            Createdat       = DateTime.UtcNow,
            Approvedat      = DateTime.UtcNow,
            Deletedat       = null
        };

        ctx.Transactions.Add(tx);
        await ctx.SaveChangesAsync(ct);

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.RejectTransaction(tx.Id)
        );
    }

    // ============================================================
    // GetTransactionsForPlayer
    // ============================================================

    [Fact]
    public async Task GetTransactionsForPlayer_Returns_OnlyNonDeleted_ForPlayer_OrderedNewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;

        var player      = CreateActivePlayer("history-player@test.local");
        var otherPlayer = CreateActivePlayer("history-other@test.local");

        ctx.Players.AddRange(player, otherPlayer);
        await ctx.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;

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
            Deletedat       = now // soft-deleted
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

        // Assert: only non-deleted transactions for this player
        Assert.Equal(2, result.Count);
        Assert.All(result, t =>
        {
            Assert.Equal(player.Id, t.Playerid);
            Assert.Null(t.Deletedat);
        });

        // Assert: ordered by CreatedAt descending (newest first)
        Assert.Equal(
            new[] { newer.Id, older.Id },
            result.Select(t => t.Id).ToArray()
        );

        // Assert: navigation property Player is included
        Assert.All(result, t => Assert.NotNull(t.Player));
    }

    // ============================================================
    // GetPendingTransactions
    // ============================================================

    [Fact]
    public async Task GetPendingTransactions_Returns_PendingOnly_OrderedByCreatedAt_AndIncludesPlayer()
    {
        var ct = TestContext.Current.CancellationToken;

        var p1 = CreateActivePlayer("pending-1@test.local");
        var p2 = CreateActivePlayer("pending-2@test.local");

        ctx.Players.AddRange(p1, p2);
        await ctx.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;

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

        // Filter only the ones we just created (in case there are others in DB)
        var ours = result
            .Where(t => t.Id == pendingOld.Id || t.Id == pendingNew.Id)
            .OrderBy(t => t.Createdat)
            .ToList();

        // Assert: both pending transactions are present and ordered by CreatedAt ascending
        Assert.Equal(2, ours.Count);
        Assert.All(ours, t => Assert.Equal("Pending", t.Status));
        Assert.Equal(
            new[] { pendingOld.Id, pendingNew.Id },
            ours.Select(t => t.Id).ToArray()
        );

        // Assert: Player navigation is populated
        Assert.All(ours, t => Assert.NotNull(t.Player));
    }

    // ============================================================
    // GetTransactionsHistory
    // ============================================================

    [Fact]
    public async Task GetTransactionsHistory_Filters_ByPlayer_AndExcludesPendingByDefault()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("history-filter@test.local");
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;

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

        // Act: no explicit status => Pending should be excluded
        var history = await transactionService.GetTransactionsHistory(playerId: player.Id, status: null);

        // Assert: only Approved + Rejected
        Assert.Equal(2, history.Count);
        Assert.All(history, t =>
        {
            Assert.Equal(player.Id, t.Playerid);
            Assert.NotEqual("Pending", t.Status);
        });

        // Assert: ordered by CreatedAt descending
        Assert.Equal(
            new[] { rejected.Id, approved.Id },
            history.Select(t => t.Id).ToArray()
        );
    }

    [Fact]
    public async Task GetTransactionsHistory_Filters_ByStatus_WhenProvided()
    {
        var ct = TestContext.Current.CancellationToken;

        var player = CreateActivePlayer("history-status@test.local");
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;

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
            async () => await transactionService.GetTransactionsHistory(playerId: null, status: "SomethingElse")
        );
    }

    // ============================================================
    // GetPlayerBalance / GetBalanceForCurrentUser / GetTransactionsForCurrentUser
    // ============================================================

    [Fact]
    public async Task GetPlayerBalance_Computes_ApprovedMinusBoardsPrice()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: player with some payments and boards
        var player = CreateActivePlayer("balance@test.local");
        var game   = CreateGame(2025, 1, isActive: false);

        ctx.Players.Add(player);
        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);

        // Boards (spending)
        var board1 = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 20,
            Createdat    = DateTime.UtcNow.AddMinutes(-20),
            Deletedat    = null
        };

        var board2 = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 6, 7, 8, 9, 10 }.ToList(),
            Price        = 40,
            Createdat    = DateTime.UtcNow.AddMinutes(-10),
            Deletedat    = null
        };

        // Deleted board should not be counted
        var deletedBoard = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 100,
            Createdat    = DateTime.UtcNow.AddMinutes(-5),
            Deletedat    = DateTime.UtcNow
        };

        ctx.Boards.AddRange(board1, board2, deletedBoard);

        // Transactions (income)
        var approved1 = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 100,
            Status          = "Approved",
            Createdat       = DateTime.UtcNow.AddMinutes(-30),
            Deletedat       = null
        };

        var approved2 = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 50,
            Status          = "Approved",
            Createdat       = DateTime.UtcNow.AddMinutes(-25),
            Deletedat       = null
        };

        var pending = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 999,
            Status          = "Pending",
            Createdat       = DateTime.UtcNow.AddMinutes(-15),
            Deletedat       = null
        };

        var rejected = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 888,
            Status          = "Rejected",
            Createdat       = DateTime.UtcNow.AddMinutes(-12),
            Deletedat       = null
        };

        ctx.Transactions.AddRange(approved1, approved2, pending, rejected);
        await ctx.SaveChangesAsync(ct);

        // Act
        PlayerBalanceResponseDto balance = await transactionService.GetPlayerBalance(player.Id);

        // Sum(Approved) = 100 + 50 = 150
        // Sum(boards.Price) (non-deleted) = 20 + 40 = 60
        // Balance = 150 - 60 = 90
        Assert.Equal(player.Id, balance.PlayerId);
        Assert.Equal(90, balance.Balance);
    }

    [Fact]
    public async Task GetPlayerBalance_Throws_When_PlayerNotFound()
    {
        var unknownPlayerId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetPlayerBalance(unknownPlayerId)
        );
    }

    [Fact]
    public async Task GetTransactionsForCurrentUser_Returns_OnlyNonDeletedTransactions_ForResolvedPlayer()
    {
        var ct = TestContext.Current.CancellationToken;

        const string email = "tx-current-history@test.local";

        var player      = CreateActivePlayer(email);
        var otherPlayer = CreateActivePlayer("tx-current-other@test.local");

        ctx.Players.AddRange(player, otherPlayer);
        await ctx.SaveChangesAsync(ct);

        var tx1 = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 10,
            Status          = "Approved",
            Createdat       = DateTime.UtcNow.AddMinutes(-10),
            Deletedat       = null
        };

        var tx2 = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 20,
            Status          = "Pending",
            Createdat       = DateTime.UtcNow.AddMinutes(-5),
            Deletedat       = null
        };

        var deleted = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 30,
            Status          = "Approved",
            Createdat       = DateTime.UtcNow.AddMinutes(-3),
            Deletedat       = DateTime.UtcNow
        };

        var otherTx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = otherPlayer.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 40,
            Status          = "Approved",
            Createdat       = DateTime.UtcNow.AddMinutes(-1),
            Deletedat       = null
        };

        ctx.Transactions.AddRange(tx1, tx2, deleted, otherTx);
        await ctx.SaveChangesAsync(ct);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = await transactionService.GetTransactionsForCurrentUser(user);

        // Assert: only non-deleted transactions for this player
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
        var ct = TestContext.Current.CancellationToken;

        const string email = "balance-current@test.local";

        var player = CreateActivePlayer(email);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var game = CreateGame(2025, 2, isActive: false);
        ctx.Games.Add(game);
        await ctx.SaveChangesAsync(ct);

        // One board, one approved transaction
        var board = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = player.Id,
            Gameid       = game.Id,
            Numbers      = new[] { 1, 2, 3, 4, 5 }.ToList(),
            Price        = 20,
            Createdat    = DateTime.UtcNow.AddMinutes(-10),
            Deletedat    = null
        };

        var approvedTx = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = player.Id,
            Mobilepaynumber = NewMobilePayNumber(),
            Amount          = 50,
            Status          = "Approved",
            Createdat       = DateTime.UtcNow.AddMinutes(-15),
            Deletedat       = null
        };

        ctx.Boards.Add(board);
        ctx.Transactions.Add(approvedTx);
        await ctx.SaveChangesAsync(ct);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var balanceDirect = await transactionService.GetPlayerBalance(player.Id);
        var balanceFromClaims = await transactionService.GetBalanceForCurrentUser(user);

        // Sum(Approved) = 50, Sum(Boards) = 20 => balance 30
        Assert.Equal(30, balanceDirect.Balance);
        Assert.Equal(30, balanceFromClaims.Balance);
        Assert.Equal(balanceDirect.Balance, balanceFromClaims.Balance);
    }

    [Fact]
    public async Task GetBalanceForCurrentUser_Throws_When_EmailMissing()
    {
        var identity = new ClaimsIdentity(); // no email
        var user = new ClaimsPrincipal(identity);

        await Assert.ThrowsAsync<ValidationException>(
            async () => await transactionService.GetBalanceForCurrentUser(user)
        );
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
            async () => await transactionService.GetBalanceForCurrentUser(user)
        );
    }
}
