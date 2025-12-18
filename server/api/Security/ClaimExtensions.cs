using System.Security.Claims;
using dataccess.Entities;

namespace Api.Security;

public static class ClaimExtensions
{
    public static string GetUserId(this ClaimsPrincipal claims) =>
        claims.FindFirst(ClaimTypes.NameIdentifier)?.Value   
        ?? claims.FindFirst("sub")?.Value                   
        ?? throw new InvalidOperationException("No user id claim found in token.");

    public static string? GetUserRole(this ClaimsPrincipal claims) =>
        claims.FindFirst(ClaimTypes.Role)?.Value
        ?? claims.FindFirst("role")?.Value;

    public static IEnumerable<Claim> ToClaims(this User user) =>
        new[]
        {
            new Claim("sub",  user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role,  user.Role),
        };
}