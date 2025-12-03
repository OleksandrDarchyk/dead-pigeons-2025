using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using api.Models;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ValidationException = Bogus.ValidationException;

namespace api.Services;

public class AuthService(
    MyDbContext ctx,
    ILogger<AuthService> logger,
    TimeProvider timeProvider,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService
) : IAuthService
{
    // Build a simple DTO from claims on the current principal
    public JwtClaims GetCurrentUserClaims(ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole() ?? string.Empty;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        return new JwtClaims
        {
            Id = userId,
            Email = email,
            Role = role
        };
    }

    public async Task<JwtResponse> Login(LoginRequestDto dto)
    {
        // Let DataAnnotations guard basic shape (required fields etc.)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // Only allow non-deleted accounts to log in
        var user = await ctx.Users
            .FirstOrDefaultAsync(u =>
                u.Email == dto.Email &&
                u.Deletedat == null);

        if (user == null)
        {
            // Still return a 400, but with a clear message for the player
            logger.LogWarning("Login failed: user with email {Email} not found", dto.Email);
            throw new ValidationException(
                "No account found for this email. Please register first.");
        }

        // Verify password against Argon2id hash
        var result = passwordHasher.VerifyHashedPassword(user, user.Passwordhash, dto.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login failed: incorrect password for {Email}", dto.Email);
            throw new ValidationException("Password is incorrect!");
        }

        // Optionally handle SuccessRehashNeeded the same as Success
        logger.LogInformation("User {Email} logged in successfully", dto.Email);

        var token = tokenService.CreateToken(user);
        return new JwtResponse(token);
    }

    public async Task<JwtResponse> Register(RegisterRequestDto dto)
    {
        // Validate basic shape (email format, password length etc.)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // Player must already exist in the system with this email
        var playerExists = await ctx.Players
            .AnyAsync(p =>
                p.Email == dto.Email &&
                p.Deletedat == null);

        if (!playerExists)
        {
            // Do not allow random people to create accounts
            throw new ValidationException(
                "You must be registered as a player by the club before creating an account.");
        }

        // A non-deleted user with this email must not already exist
        var emailTaken = await ctx.Users
            .AnyAsync(u =>
                u.Email == dto.Email &&
                u.Deletedat == null);

        if (emailTaken)
        {
            throw new ValidationException("An account with this email already exists.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = dto.Email,
            Createdat = now,
            Role = Roles.User,
            Salt = string.Empty // Argon2id hash contains its own salt
        };

        // Hash the raw password using Argon2id
        user.Passwordhash = passwordHasher.HashPassword(user, dto.Password);

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        logger.LogInformation("User {Email} registered successfully", dto.Email);

        // New users receive a JWT right after registration
        var token = tokenService.CreateToken(user);
        return new JwtResponse(token);
    }
}
