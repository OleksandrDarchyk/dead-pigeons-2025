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
            // Still return a 400, but we do not reveal more details than needed
            logger.LogWarning("Login failed: user with email {Email} not found", dto.Email);
            throw new ValidationException("User is not found!");
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
        // Validate incoming data using DataAnnotations
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // Check for duplicate email among non-deleted users
        var isEmailTaken = await ctx.Users
            .AnyAsync(u =>
                u.Email == dto.Email &&
                u.Deletedat == null);

        if (isEmailTaken)
        {
            throw new ValidationException("Email is already taken");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = dto.Email,
            Createdat = now,
            // Use shared role constant instead of hard-coded string
            Role = Roles.User,
            // Salt column kept for compatibility, but Argon2 hash embeds its own salt
            Salt = string.Empty
        };

        // Hash password with the configured Argon2id hasher
        user.Passwordhash = passwordHasher.HashPassword(user, dto.Password);

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        logger.LogInformation("User {Email} registered successfully", dto.Email);

        // New users receive a token directly after registration
        var token = tokenService.CreateToken(user);
        return new JwtResponse(token);
    }
}
