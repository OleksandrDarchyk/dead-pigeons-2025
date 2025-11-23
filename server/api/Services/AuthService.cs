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
    ITokenService tokenService // JwtService injected here
) : IAuthService
{
    // Used by WhoAmI (via ClaimsPrincipal + JwtBearer)
    public JwtClaims GetCurrentUserClaims(ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();                 // from ClaimExtensions (sub)
        var role = principal.GetUserRole() ?? string.Empty; // also from ClaimExtensions
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
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            throw new ValidationException("User is not found!");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.Passwordhash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new ValidationException("Password is incorrect!");
        }

        // Create JWT via JwtService
        var token = tokenService.CreateToken(user);
        return new JwtResponse(token);
    }

    public async Task<JwtResponse> Register(RegisterRequestDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var isEmailTaken = await ctx.Users.AnyAsync(u => u.Email == dto.Email);
        if (isEmailTaken)
        {
            throw new ValidationException("Email is already taken");
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = dto.Email,
            Createdat = timeProvider.GetUtcNow().DateTime.ToUniversalTime(),
            Role = "User",
            Salt = string.Empty // column still exists, but hash contains salt
        };

        // Hash password with Argon2id password hasher
        user.Passwordhash = passwordHasher.HashPassword(user, dto.Password);

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Issue JWT for newly registered user
        var token = tokenService.CreateToken(user);
        return new JwtResponse(token);
    }
}
