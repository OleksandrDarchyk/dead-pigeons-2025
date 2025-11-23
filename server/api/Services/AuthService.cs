using System.ComponentModel.DataAnnotations;

using System.Text.Json;
using api.Models;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Serializers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ValidationException = Bogus.ValidationException;

namespace api.Services;

public class AuthService(
    MyDbContext ctx,
    ILogger<AuthService> logger,
    TimeProvider timeProvider,
    AppOptions appOptions,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<JwtClaims> VerifyAndDecodeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ValidationException("No token attached!");

        const string bearerPrefix = "Bearer ";
        if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring(bearerPrefix.Length);
        }

        var builder = CreateJwtBuilder();

        string jsonString;
        try
        {
            jsonString = builder.Decode(token)
                         ?? throw new ValidationException("Authentication failed!");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to decode JWT");
            throw new ValidationException("Failed to verify JWT");
        }

        var jwtClaims = JsonSerializer.Deserialize<JwtClaims>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new ValidationException("Authentication failed!");

        var userExists = await ctx.Users.AnyAsync(u => u.Id == jwtClaims.Id);
        if (!userExists)
        {
            throw new ValidationException("Authentication is valid, but user is not found!");
        }

        return jwtClaims;
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

        var token = CreateJwt(user);
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
            Salt = string.Empty
        };

        user.Passwordhash = passwordHasher.HashPassword(user, dto.Password);

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var token = CreateJwt(user);
        return new JwtResponse(token);
    }


    private JwtBuilder CreateJwtBuilder()
    {
        return JwtBuilder.Create()
            .WithAlgorithm(new HMACSHA512Algorithm())
            .WithSecret(appOptions.JwtSecret)
            .WithUrlEncoder(new JwtBase64UrlEncoder())
            .WithJsonSerializer(new JsonNetSerializer())
            .MustVerifySignature();
    }

    private string CreateJwt(User user)
    {
        return CreateJwtBuilder()
            .AddClaim(nameof(User.Id), user.Id)
            .AddClaim(nameof(User.Email), user.Email)
            .AddClaim(nameof(User.Role), user.Role)
            .Encode();
    }
}
