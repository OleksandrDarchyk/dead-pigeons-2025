using api.Models.Requests;
using Api.Security;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using ValidationException = Bogus.ValidationException;

namespace tests;

/// <summary>
/// Service-level tests for AuthService (login, register, token verification).
/// These tests work directly against the real database in a transaction.
/// </summary>
public class AuthServiceTests(
    IAuthService authService,
    MyDbContext ctx,
    TestTransactionScope transaction,
    TimeProvider timeProvider,
    ITestOutputHelper output) : IAsyncLifetime
{
    /// <summary>
    /// Each test runs in its own transaction which gets rolled back afterwards.
    /// Also resets FakeTimeProvider because other test classes may change it.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await transaction.BeginTransactionAsync(ct);

        // IMPORTANT:
        // Other tests (e.g. BoardService cutoff tests) may SetUtcNow(...) to a future date.
        // That can break JWT validation ("token not yet valid").
        if (timeProvider is FakeTimeProvider fake)
        {
            fake.SetUtcNow(DateTime.UtcNow);
        }
    }

    public ValueTask DisposeAsync()
    {
        // optional extra safety: reset time again
        if (timeProvider is FakeTimeProvider fake)
        {
            fake.SetUtcNow(DateTime.UtcNow);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Helper for creating a Player entity with a given email and active/inactive status.
    /// </summary>
    private Player CreatePlayer(string email, bool isActive = true)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new Player
        {
            Id = Guid.NewGuid().ToString(),
            Fullname = "Test Player",
            Email = email,
            Phone = "12345678",
            Isactive = isActive,
            Activatedat = isActive ? now : null,
            Createdat = now,
            Deletedat = null
        };
    }

    [Fact]
    public async Task Register_CreatesUser_ForExistingActivePlayer()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid()}@test.local";
        const string password = "StrongPassword123!";

        // Arrange: create active player with this email
        var player = CreatePlayer(email, isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        };

        // Act
        var response = await authService.Register(dto);

        // Assert: token was returned
        Assert.False(string.IsNullOrWhiteSpace(response.Token));

        // Email is stored in normalized (lowercase) form
        var normalized = email.Trim().ToLowerInvariant();
        var user = await ctx.Users.SingleAsync(u => u.Email == normalized, ct);

        Assert.Equal(Roles.User, user.Role);
        Assert.NotEqual(password, user.Passwordhash);

        output.WriteLine($"[Auth] Registered user with email: {user.Email}, id: {user.Id}");
    }

    [Fact]
    public async Task Register_Throws_When_PlayerDoesNotExist()
    {
        var email = $"{Guid.NewGuid()}@test.local";

        var dto = new RegisterRequestDto
        {
            Email = email,
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!"
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.Register(dto));
    }

    [Fact]
    public async Task Register_Throws_When_PlayerIsInactive()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid()}@test.local";

        // Player exists but is inactive
        var player = CreatePlayer(email, isActive: false);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new RegisterRequestDto
        {
            Email = email,
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!"
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.Register(dto));
    }

    [Fact]
    public async Task Register_IsCaseInsensitive_ForEmail()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid()}@test.local";

        var player = CreatePlayer(email.ToLower(), isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new RegisterRequestDto
        {
            Email = email.ToUpper(),
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!"
        };

        var response = await authService.Register(dto);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task Register_Throws_When_EmailAlreadyHasUser()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid()}@test.local";
        const string password = "StrongPassword123!";

        var player = CreatePlayer(email, isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        };

        await authService.Register(dto);

        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.Register(dto));
    }

    [Fact]
    public async Task Login_Succeeds_ForValidCredentials()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid()}@test.local";
        const string password = "ValidPassword123!";

        var player = CreatePlayer(email, isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        await authService.Register(new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        var loginDto = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        var response = await authService.Login(loginDto);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task Login_Throws_When_UserNotFound()
    {
        var email = $"{Guid.NewGuid()}@test.local";

        var dto = new LoginRequestDto
        {
            Email = email,
            Password = "SomePassword123!"
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.Login(dto));
    }

    [Fact]
    public async Task Login_Throws_When_PasswordIncorrect()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid()}@test.local";
        const string correctPassword = "CorrectPassword123!";

        var player = CreatePlayer(email, isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        await authService.Register(new RegisterRequestDto
        {
            Email = email,
            Password = correctPassword,
            ConfirmPassword = correctPassword
        });

        var loginDto = new LoginRequestDto
        {
            Email = email,
            Password = "WrongPassword123!"
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.Login(loginDto));
    }

    [Fact]
    public async Task Login_IsCaseInsensitive_ForEmail()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid()}@test.local";
        const string password = "ValidPassword123!";

        var player = CreatePlayer(email.ToLower(), isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        await authService.Register(new RegisterRequestDto
        {
            Email = email.ToLower(),
            Password = password,
            ConfirmPassword = password
        });

        var loginDto = new LoginRequestDto
        {
            Email = email.ToUpper(),
            Password = password
        };

        var response = await authService.Login(loginDto);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task VerifyAndDecodeToken_Returns_ValidClaims()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid()}@test.local";
        const string password = "StrongPassword123!";

        var player = CreatePlayer(email, isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var response = await authService.Register(new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        var claims = await authService.VerifyAndDecodeToken(response.Token);

        Assert.Equal(email.ToLower(), claims.Email.ToLower());
        Assert.Equal(Roles.User, claims.Role);
        Assert.False(string.IsNullOrWhiteSpace(claims.Id));
    }

    [Fact]
    public async Task VerifyAndDecodeToken_Throws_For_InvalidToken()
    {
        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.VerifyAndDecodeToken("totally-invalid-token"));
    }
}
