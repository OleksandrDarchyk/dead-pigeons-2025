using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using api.Models;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ValidationException = Bogus.ValidationException;

namespace api.Services;

/// <summary>
/// Authentication service for login, registration and token verification.
/// </summary>
public class AuthService(
    MyDbContext ctx,
    ILogger<AuthService> logger,
    TimeProvider timeProvider,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService
) : IAuthService
{
    /// <summary>
    /// Normalizes email so we can do case-insensitive comparisons.
    /// </summary>
    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Reads user claims (id, email, role) from the current principal and returns them as a DTO.
    /// </summary>
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

    /// <summary>
    /// Logs a user in with email and password.
    /// </summary>
    public async Task<JwtResponse> Login(LoginRequestDto dto)
    {
        // Validate basic DTO shape (required fields, etc.)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var normalizedEmail = NormalizeEmail(dto.Email);

        // Find non-deleted user by email, case-insensitive
        var user = await ctx.Users
            .Where(u => u.Deletedat == null)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user is null)
        {
            logger.LogWarning("Login failed: user with email {Email} not found", dto.Email);
            throw new ValidationException("No account found for this email. Please register first.");
        }

        // Verify password against Argon2id hash
        var result = passwordHasher.VerifyHashedPassword(user, user.Passwordhash, dto.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login failed: incorrect password for {Email}", dto.Email);
            throw new ValidationException("Password is incorrect!");
        }

        logger.LogInformation("User {Email} logged in successfully", user.Email);

        var token = tokenService.CreateToken(user);
        return new JwtResponse(token);
    }

    /// <summary>
    /// Registers a new user account for an existing active player.
    /// </summary>
    public async Task<JwtResponse> Register(RegisterRequestDto dto)
    {
        // Validate basic DTO rules (email format, password length, etc.)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var normalizedEmail = NormalizeEmail(dto.Email);

        // Player must exist with this email (case-insensitive), not soft-deleted
        var player = await ctx.Players
            .Where(p => p.Deletedat == null)
            .FirstOrDefaultAsync(p => p.Email.ToLower() == normalizedEmail);

        if (player is null)
        {
            // Do not allow random people to create accounts
            throw new ValidationException(
                "You must be registered as a player by the club before creating an account.");
        }

        // Player must be active
        if (!player.Isactive)
        {
            throw new ValidationException(
                "Your membership is not active yet. Please contact the club.");
        }

        // Email must not already be taken by a non-deleted user (case-insensitive)
        var emailTaken = await ctx.Users
            .Where(u => u.Deletedat == null)
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail);

        if (emailTaken)
        {
            throw new ValidationException("An account with this email already exists.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = normalizedEmail,      // store normalized email
            Createdat = now,
            Role = Roles.User,
        };

        // Hash the raw password using Argon2id
        user.Passwordhash = passwordHasher.HashPassword(user, dto.Password);

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        logger.LogInformation("User {Email} registered successfully", user.Email);

        // New users get a JWT right after registration
        var token = tokenService.CreateToken(user);
        return new JwtResponse(token);
    }

    /// <summary>
    /// Verifies a JWT token and returns decoded claims,
    /// or throws a validation error if the token is invalid/expired.
    /// </summary>
    public Task<JwtClaims> VerifyAndDecodeToken(string token)
    {
        try
        {
            var claims = tokenService.ValidateAndDecode(token);
            return Task.FromResult(claims);
        }
        catch (SecurityTokenException)
        {
            // Map low-level token error to a domain validation exception
            throw new ValidationException("Invalid or expired token.");
        }
    }

}
