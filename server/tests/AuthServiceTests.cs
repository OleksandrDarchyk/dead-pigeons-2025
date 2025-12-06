// server/tests/AuthServiceTests.cs

using api.Models.Requests;
using Api.Security;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;


// Domain-level validation in services uses Bogus.ValidationException
using ValidationException = Bogus.ValidationException;

namespace tests;

public class AuthServiceTests(
    IAuthService authService,
    MyDbContext ctx,
    ITestOutputHelper output)
{
    // Helper: create a player row for a given email
    private static Player CreatePlayer(string email, bool isActive = true)
    {
        var now = DateTime.UtcNow;

        return new Player
        {
            Id          = Guid.NewGuid().ToString(),
            Fullname    = "Auth Test Player",
            Email       = email,
            Phone       = "12345678",
            Isactive    = isActive,
            Activatedat = isActive ? now : null,
            Createdat   = now,
            Deletedat   = null
        };
    }

    // ============================================================
    // Register
    // ============================================================

    [Fact]
    public async Task Register_CreatesUser_ForExistingPlayer()
    {
        var ct = TestContext.Current.CancellationToken;

        // Use a unique email to avoid conflicts with seed data
        var email = $"{Guid.NewGuid()}@auth-tests.local";
        const string password = "VeryStrongPassword123!";

        // Arrange: player already exists in the system
        var player = CreatePlayer(email);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var dto = new RegisterRequestDto
        {
            Email           = email,
            Password        = password,
            ConfirmPassword = password // ← додали для проходження DataAnnotations
        };

        // Act
        var response = await authService.Register(dto);

        // Assert: JWT is returned
        Assert.False(string.IsNullOrWhiteSpace(response.Token));

        // Assert: user row is created
        var user = await ctx.Users.SingleAsync(u => u.Email == email, ct);

        Assert.Equal(email, user.Email);
        Assert.Equal(Roles.User, user.Role);
        Assert.NotNull(user.Createdat);
        Assert.Null(user.Deletedat);

        // Password is hashed, not stored as plain text
        Assert.False(string.IsNullOrWhiteSpace(user.Passwordhash));
        Assert.NotEqual(password, user.Passwordhash);

        output.WriteLine($"Registered user id: {user.Id}, email: {user.Email}");
    }

    [Fact]
    public async Task Register_Throws_When_PlayerDoesNotExist()
    {
        // Use a unique email that is not present in Players table
        var email = $"{Guid.NewGuid()}@auth-tests.local";
        const string password = "VeryStrongPassword123!";

        var dto = new RegisterRequestDto
        {
            Email           = email,
            Password        = password,
            ConfirmPassword = password // ← теж додаємо, щоб впала саме доменна валідація
        };

        // Act + Assert:
        // DTO is valid, but there is no Player with this email,
        // so the domain rule should throw Bogus.ValidationException.
        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.Register(dto)
        );
    }

    // ============================================================
    // Login
    // ============================================================

    [Fact]
    public async Task Login_Throws_When_UserNotFound()
    {
        // Use a unique email that has no User row
        var email = $"{Guid.NewGuid()}@auth-tests.local";

        var dto = new LoginRequestDto
        {
            Email    = email,
            Password = "SomeStrongPassword123!"
        };

        // Act + Assert: no such user -> domain ValidationException
        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.Login(dto)
        );
    }

    [Fact]
    public async Task Login_Throws_When_PasswordIncorrect()
    {
        var ct = TestContext.Current.CancellationToken;

        var email = $"{Guid.NewGuid()}@auth-tests.local";
        const string correctPassword = "CorrectPassword123!";
        const string wrongPassword   = "WrongPassword123!";

        // Arrange: existing player + register user
        var player = CreatePlayer(email);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var registerDto = new RegisterRequestDto
        {
            Email           = email,
            Password        = correctPassword,
            ConfirmPassword = correctPassword // ← додали
        };

        await authService.Register(registerDto);

        // Act + Assert: login with wrong password must fail
        var loginDto = new LoginRequestDto
        {
            Email    = email,
            Password = wrongPassword
        };

        await Assert.ThrowsAsync<ValidationException>(
            async () => await authService.Login(loginDto)
        );
    }

    [Fact]
    public async Task Login_Succeeds_ForValidCredentials()
    {
        var ct = TestContext.Current.CancellationToken;

        var email = $"{Guid.NewGuid()}@auth-tests.local";
        const string password = "ValidPassword123!";

        // Arrange: create player and then register user
        var player = CreatePlayer(email);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var registerDto = new RegisterRequestDto
        {
            Email           = email,
            Password        = password,
            ConfirmPassword = password // ← додали
        };

        var registerResponse = await authService.Register(registerDto);
        Assert.False(string.IsNullOrWhiteSpace(registerResponse.Token));

        // Act: try to login with the same credentials
        var loginDto = new LoginRequestDto
        {
            Email    = email,
            Password = password
        };

        var loginResponse = await authService.Login(loginDto);

        // Assert: new JWT is generated on login
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.Token));

        output.WriteLine($"Login JWT: {loginResponse.Token}");
    }

    // ============================================================
    // VerifyAndDecodeToken / GetCurrentUserClaims
    // ============================================================

    [Fact]
    public async Task VerifyAndDecodeToken_Returns_Claims_ForRegisteredUser()
    {
        var ct = TestContext.Current.CancellationToken;

        var email = $"{Guid.NewGuid()}@auth-tests.local";
        const string password = "StrongPassword123!";

        // Arrange: existing player and registered user
        var player = CreatePlayer(email);
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var registerDto = new RegisterRequestDto
        {
            Email           = email,
            Password        = password,
            ConfirmPassword = password // ← додали
        };

        var registerResponse = await authService.Register(registerDto);

        // Act: decode JWT using the service
        var claims = await authService.VerifyAndDecodeToken(registerResponse.Token);

        // Assert: claims match the registered user
        Assert.Equal(email, claims.Email);
        Assert.Equal(Roles.User, claims.Role);
        Assert.False(string.IsNullOrWhiteSpace(claims.Id));

        output.WriteLine($"Decoded claims: id={claims.Id}, email={claims.Email}, role={claims.Role}");
    }
}
