// tests/PlayerServiceTests.cs
using api.Models.Requests;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = Bogus.ValidationException;

namespace tests.Services;

/// <summary>
/// Service-level tests for <see cref="PlayerService"/>.
/// Verifies create, read, update, soft delete, filtering and sorting logic.
/// Each test runs in its own transaction for full isolation.
/// </summary>
public class PlayerServiceTests(
    IPlayerService playerService,
    MyDbContext ctx,
    TimeProvider timeProvider,
    ITestOutputHelper outputHelper,
    TestTransactionScope transaction) : IAsyncLifetime
{
    /// <summary>
    /// Start a new database transaction before each test.
    /// All changes will be rolled back by <see cref="TestTransactionScope"/>.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await transaction.BeginTransactionAsync(ct);
    }

    /// <summary>
    /// No extra async cleanup needed here.
    /// Transaction rollback happens inside <see cref="TestTransactionScope.Dispose"/>.
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ---------------------------------------------------------------------
    // Helper methods
    // ---------------------------------------------------------------------

    /// <summary>
    /// Helper to create a Player entity with reasonable defaults.
    /// Uses TimeProvider so tests remain deterministic and do not depend on DateTime.UtcNow.
    /// </summary>
    private Player CreatePlayer(
        string fullName,
        string email,
        string phone,
        bool isActive,
        bool isDeleted = false)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new Player
        {
            Id = Guid.NewGuid().ToString(),
            Fullname = fullName,
            Email = email,
            Phone = phone,
            Isactive = isActive,
            Activatedat = isActive ? now : null,
            Createdat = now,
            Deletedat = isDeleted ? now : null
        };
    }

    // ============================================================
    // CreatePlayer
    // ============================================================

    [Fact]
    public async Task CreatePlayer_Creates_InactivePlayer_WithUniqueEmail()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: ensure email is not taken
        const string email = "new-player@test.local";

        var dto = new CreatePlayerRequestDto
        {
            FullName = "New Player",
            Email = email,
            Phone = "12345678"
        };

        // Act
        var created = await playerService.CreatePlayer(dto);

        // Assert: basic properties and invariants
        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        Assert.Equal(dto.FullName, created.Fullname);
        Assert.Equal(dto.Email, created.Email);
        Assert.Equal(dto.Phone, created.Phone);

        // New players must start inactive by business rule
        Assert.False(created.Isactive);
        Assert.Null(created.Activatedat);
        Assert.Null(created.Deletedat);
        Assert.NotEqual(default, created.Createdat);

        // Player exists in database
        var fromDb = await ctx.Players.SingleAsync(p => p.Id == created.Id, ct);
        Assert.Equal(created.Email, fromDb.Email);

        outputHelper.WriteLine($"[Player] Created player: {created.Id} ({created.Email})");
    }

    [Fact]
    public async Task CreatePlayer_Throws_When_EmailAlreadyExists()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: existing non-deleted player with email
        const string email = "duplicate@test.local";

        var existing = CreatePlayer("Existing", email, "11111111", isActive: true);
        ctx.Players.Add(existing);
        await ctx.SaveChangesAsync(ct);

        var dto = new CreatePlayerRequestDto
        {
            FullName = "Another",
            Email = email,
            Phone = "22222222"
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.CreatePlayer(dto));
    }

   

    [Fact]
    public async Task CreatePlayer_Throws_When_DtoInvalid()
    {
        // This test depends on DataAnnotations existing on CreatePlayerRequestDto.
        // If your DTO has no validation attributes, this test should be removed or adjusted.
        var dto = new CreatePlayerRequestDto
        {
            FullName = "A",
            Email = "not-an-email",
            Phone = "1"
        };

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            async () => await playerService.CreatePlayer(dto));
    }

    // ============================================================
    // GetPlayers (filtering + ordering)
    // ============================================================

    [Fact]
    public async Task GetPlayers_Filters_By_IsActive_And_DefaultSortsByFullNameAsc()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: active + inactive + soft-deleted
        var active1 = CreatePlayer("Alice", "alice@test.local", "11111111", isActive: true);
        var active2 = CreatePlayer("Charlie", "charlie@test.local", "22222222", isActive: true);
        var inactive = CreatePlayer("Bob", "bob@test.local", "33333333", isActive: false);
        var deleted = CreatePlayer("Deleted", "deleted@test.local", "44444444", isActive: true, isDeleted: true);

        ctx.Players.AddRange(active1, active2, inactive, deleted);
        await ctx.SaveChangesAsync(ct);

        // Act: only active players, default sorting (Fullname ascending)
        var result = await playerService.GetPlayers(isActive: true);

        // Assert: soft-deleted player is excluded
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.Isactive));
        Assert.DoesNotContain(result, p => p.Deletedat != null);

        // Default ordering is by Fullname ascending
        Assert.Equal(
            new[] { "Alice", "Charlie" },
            result.Select(p => p.Fullname).ToArray());
    }

    [Fact]
    public async Task GetPlayers_Sorts_By_Email_Descending()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var p1 = CreatePlayer("One", "a@test.local", "11111111", isActive: true);
        var p2 = CreatePlayer("Two", "c@test.local", "22222222", isActive: true);
        var p3 = CreatePlayer("Three", "b@test.local", "33333333", isActive: true);

        ctx.Players.AddRange(p1, p2, p3);
        await ctx.SaveChangesAsync(ct);

        // Act
        var result = await playerService.GetPlayers(
            isActive: null,
            sortBy: "email",
            direction: "desc");

        // Assert: ordered by email (c, b, a)
        Assert.Equal(
            new[] { "c@test.local", "b@test.local", "a@test.local" },
            result.Select(p => p.Email).ToArray());
    }

    // ============================================================
    // ActivatePlayer
    // ============================================================

    [Fact]
    public async Task ActivatePlayer_Sets_IsActive_And_ActivatedAt_When_Inactive()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: inactive player with null Activatedat
        var player = CreatePlayer("Inactive", "inactive@test.local", "11111111", isActive: false);
        player.Activatedat = null;
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        // Act: first activation
        var activated = await playerService.ActivatePlayer(player.Id);

        // Assert: player becomes active and gets an activation timestamp
        Assert.True(activated.Isactive);
        Assert.NotNull(activated.Activatedat);

        var firstActivatedAt = activated.Activatedat;

        // Act again: calling ActivatePlayer twice should not move Activatedat
        var activatedAgain = await playerService.ActivatePlayer(player.Id);

        Assert.True(activatedAgain.Isactive);
        Assert.Equal(firstActivatedAt, activatedAgain.Activatedat);
    }

    [Fact]
    public async Task ActivatePlayer_Throws_When_PlayerNotFound()
    {
        var unknownId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.ActivatePlayer(unknownId));
    }

    // ============================================================
    // DeactivatePlayer
    // ============================================================

    [Fact]
    public async Task DeactivatePlayer_Sets_IsActiveFalse_ForActivePlayer()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var player = CreatePlayer("Active", "active@test.local", "11111111", isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var originalActivatedAt = player.Activatedat;

        // Act
        var deactivated = await playerService.DeactivatePlayer(player.Id);

        // Assert: Isactive is false but Activatedat is preserved
        Assert.False(deactivated.Isactive);
        Assert.Equal(originalActivatedAt, deactivated.Activatedat);
    }

    [Fact]
    public async Task DeactivatePlayer_Throws_When_PlayerNotFound()
    {
        var unknownId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.DeactivatePlayer(unknownId));
    }

    // ============================================================
    // SoftDeletePlayer
    // ============================================================

    [Fact]
    public async Task SoftDeletePlayer_Sets_DeletedAt_And_PlayerIsExcludedFromQueries()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var player = CreatePlayer("To Delete", "delete@test.local", "11111111", isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        // Act
        var deleted = await playerService.SoftDeletePlayer(player.Id);

        // Assert: deleted flag is set
        Assert.NotNull(deleted.Deletedat);

        // Soft-deleted player should not be returned by GetPlayerById
        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.GetPlayerById(player.Id));

        // Soft-deleted player should not be returned by GetPlayers
        var allPlayers = await playerService.GetPlayers();
        Assert.DoesNotContain(allPlayers, p => p.Id == player.Id);
    }

    [Fact]
    public async Task SoftDeletePlayer_Throws_When_PlayerNotFoundOrAlreadyDeleted()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: already deleted player
        var player = CreatePlayer(
            "Deleted",
            "already-deleted@test.local",
            "11111111",
            isActive: true,
            isDeleted: true);

        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.SoftDeletePlayer(player.Id));
    }

    // ============================================================
    // GetPlayerById
    // ============================================================

    [Fact]
    public async Task GetPlayerById_Returns_Player_When_Exists_And_NotDeleted()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var player = CreatePlayer("Lookup", "lookup@test.local", "11111111", isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        // Act
        var result = await playerService.GetPlayerById(player.Id);

        // Assert
        Assert.Equal(player.Id, result.Id);
        Assert.Equal(player.Email, result.Email);
    }

    [Fact]
    public async Task GetPlayerById_Throws_When_PlayerDeletedOrNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: soft-deleted player
        var deleted = CreatePlayer(
            "Deleted",
            "deleted-lookup@test.local",
            "11111111",
            isActive: true,
            isDeleted: true);

        ctx.Players.Add(deleted);
        await ctx.SaveChangesAsync(ct);

        // Deleted
        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.GetPlayerById(deleted.Id));

        // Not found
        var unknownId = Guid.NewGuid().ToString();
        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.GetPlayerById(unknownId));
    }

    // ============================================================
    // UpdatePlayer
    // ============================================================

    [Fact]
    public async Task UpdatePlayer_Updates_FullName_And_Phone_When_EmailUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var player = CreatePlayer("Old Name", "update@test.local", "11111111", isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new UpdatePlayerRequestDto
        {
            Id = player.Id,
            FullName = "New Name",
            Email = player.Email, // unchanged
            Phone = "99999999"
        };

        // Act
        var updated = await playerService.UpdatePlayer(dto);

        // Assert
        Assert.Equal("New Name", updated.Fullname);
        Assert.Equal("99999999", updated.Phone);
        Assert.Equal(player.Email, updated.Email);
    }

    [Fact]
    public async Task UpdatePlayer_Updates_Email_When_NewEmailIsUnique()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var player = CreatePlayer("Target", "old@test.local", "11111111", isActive: true);
        var otherPlayer = CreatePlayer("Other", "other@test.local", "22222222", isActive: true);

        ctx.Players.AddRange(player, otherPlayer);
        await ctx.SaveChangesAsync(ct);

        var dto = new UpdatePlayerRequestDto
        {
            Id = player.Id,
            FullName = "Target",
            Email = "newunique@test.local",
            Phone = "11111111"
        };

        // Act
        var updated = await playerService.UpdatePlayer(dto);

        // Assert
        Assert.Equal("newunique@test.local", updated.Email);
    }

    [Fact]
    public async Task UpdatePlayer_Throws_When_NewEmailIsTakenByAnotherPlayer()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var player = CreatePlayer("Target", "target@test.local", "11111111", isActive: true);
        var otherPlayer = CreatePlayer("Other", "taken@test.local", "22222222", isActive: true);

        ctx.Players.AddRange(player, otherPlayer);
        await ctx.SaveChangesAsync(ct);

        var dto = new UpdatePlayerRequestDto
        {
            Id = player.Id,
            FullName = "Target",
            Email = "taken@test.local", // duplicate
            Phone = "11111111"
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.UpdatePlayer(dto));
    }
    
    [Fact]
    public async Task UpdatePlayer_Throws_When_PlayerNotFoundOrDeleted()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: a deleted player that should not be updatable
        var deleted = CreatePlayer(
            fullName: "Deleted Player",
            email: "deleted-update@test.local",
            phone: "11111111",
            isActive: true,
            isDeleted: true);

        ctx.Players.Add(deleted);
        await ctx.SaveChangesAsync(ct);

        var validDtoForDeleted = new UpdatePlayerRequestDto
        {
            Id = deleted.Id,
            FullName = "Valid Name", // assumes MinLength(3)
            Email = deleted.Email,
            Phone = deleted.Phone
        };

        // Deleted player -> domain ValidationException("Player not found.")
        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.UpdatePlayer(validDtoForDeleted));

        // Unknown id -> domain ValidationException("Player not found.")
        var validDtoForUnknown = new UpdatePlayerRequestDto
        {
            Id = Guid.NewGuid().ToString(),
            FullName = "Another Valid Name",
            Email = "unknown-update@test.local",
            Phone = "22222222"
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await playerService.UpdatePlayer(validDtoForUnknown));
    }
}
