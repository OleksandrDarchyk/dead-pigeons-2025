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

public class AuthService(
    MyDbContext ctx,
    ILogger<AuthService> logger,
    TimeProvider timeProvider,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService
) : IAuthService
{
    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
    
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
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var normalizedEmail = NormalizeEmail(dto.Email);

        var user = await ctx.Users
            .Where(u => u.Deletedat == null)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user is null)
        {
            logger.LogWarning("Login failed: user with email {Email} not found", dto.Email);
            throw new ValidationException("No account found for this email. Please register first.");
        }

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
    
    public async Task<JwtResponse> Register(RegisterRequestDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var normalizedEmail = NormalizeEmail(dto.Email);

        var player = await ctx.Players
            .Where(p => p.Deletedat == null)
            .FirstOrDefaultAsync(p => p.Email.ToLower() == normalizedEmail);

        if (player is null)
        {
            throw new ValidationException(
                "You must be registered as a player by the club before creating an account.");
        }

        if (!player.Isactive)
        {
            throw new ValidationException(
                "Your membership is not active yet. Please contact the club.");
        }

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
            Email = normalizedEmail,      
            Createdat = now,
            Role = Roles.User,
        };

        user.Passwordhash = passwordHasher.HashPassword(user, dto.Password);

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        logger.LogInformation("User {Email} registered successfully", user.Email);

        var token = tokenService.CreateToken(user);
        return new JwtResponse(token);
    }
    
    public Task<JwtClaims> VerifyAndDecodeToken(string token)
    {
        try
        {
            var claims = tokenService.ValidateAndDecode(token);
            return Task.FromResult(claims);
        }
        catch (SecurityTokenException)
        {
            throw new ValidationException("Invalid or expired token.");
        }
    }

}
