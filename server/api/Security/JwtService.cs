using System.Security.Claims;
using api.Models;
using dataccess.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Api.Security;

public interface ITokenService
{
    string CreateToken(User user);
    
    JwtClaims ValidateAndDecode(string token);

}

public class JwtService(IConfiguration configuration, TimeProvider timeProvider) : ITokenService
{
    public const string SignatureAlgorithm = SecurityAlgorithms.HmacSha512;
    private const string JwtSecretPath = "AppOptions:JwtSecret";

    private byte[] GetKeyBytes()
    {
        var secretBase64 = configuration[JwtSecretPath];

        if (string.IsNullOrWhiteSpace(secretBase64))
        {
            throw new InvalidOperationException($"{JwtSecretPath} is not configured");
        }

        // FIX: simple call, no named argument
        return Convert.FromBase64String(secretBase64);
    }

    public string CreateToken(User user)
    {
        var keyBytes = GetKeyBytes();
        var key = new SymmetricSecurityKey(keyBytes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(user.ToClaims()),
            Expires = timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            SigningCredentials = new SigningCredentials(key, SignatureAlgorithm),
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(descriptor);
    }

    public static TokenValidationParameters ValidationParameters(IConfiguration configuration)
    {
        var secretBase64 = configuration[JwtSecretPath];

        if (string.IsNullOrWhiteSpace(secretBase64))
        {
            throw new InvalidOperationException($"{JwtSecretPath} is not configured");
        }

        var keyBytes = Convert.FromBase64String(secretBase64);

        return new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidAlgorithms = [SignatureAlgorithm],
            ValidateIssuerSigningKey = true,
            TokenDecryptionKey = null,

            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    }
    
    public JwtClaims ValidateAndDecode(string token)
    {
        var parameters = ValidationParameters(configuration);

        var handler = new JsonWebTokenHandler();
        var result = handler.ValidateToken(token, parameters);

        if (!result.IsValid || result.ClaimsIdentity == null)
        {
            // Тут можна логувати, але для безпеки: просто не приймаємо токен
            throw new SecurityTokenException("Invalid or expired token.");
        }

        // Створюємо ClaimsPrincipal, щоб використати твої ClaimExtensions
        var principal = new ClaimsPrincipal(result.ClaimsIdentity);

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
}
