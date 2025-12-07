using api.Models.Requests;
using Api.Security;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
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
    ITestOutputHelper output) : IAsyncLifetime
{
    /// <summary>
    /// Each test runs in its own transaction which gets rolled back afterwards.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await transaction.BeginTransactionAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Helper for creating a Player entity with a given email and active/inactive status.
    /// </summary>
    private static Player CreatePlayer(string email, bool isActive = true)
    {
        var now = DateTime.UtcNow;
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

        // No player with this email -> registration must fail
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

        // Player email is stored in lower-case
        var player = CreatePlayer(email.ToLower(), isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new RegisterRequestDto
        {
            // User types the same email but upper-case
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

        // Existing active player
        var player = CreatePlayer(email, isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        };

        // First registration succeeds
        await authService.Register(dto);

        // Second registration with same email must fail
        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.Register(dto));
    }

    [Fact]
    public async Task Login_Succeeds_ForValidCredentials()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid()}@test.local";
        const string password = "ValidPassword123!";

        // Player exists and is active
        var player = CreatePlayer(email, isActive: true);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        // Register user first
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

        // No user with this email -> login must fail
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

        // Register with correct password
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

        // Register using lower-case email
        await authService.Register(new RegisterRequestDto
        {
            Email = email.ToLower(),
            Password = password,
            ConfirmPassword = password
        });

        // Login using upper-case version of the same email
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

        // Register user to get a valid token
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
        // Service wraps SecurityTokenException into Bogus.ValidationException
        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.VerifyAndDecodeToken("totally-invalid-token"));
    }
}
