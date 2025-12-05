using System.Security.Claims;
using api.Models;
using api.Models.Requests;

namespace api.Services;

public interface IAuthService
{
    Task<JwtResponse> Login(LoginRequestDto dto);
    Task<JwtResponse> Register(RegisterRequestDto dto);
    JwtClaims GetCurrentUserClaims(ClaimsPrincipal principal);
    
    Task<JwtClaims> VerifyAndDecodeToken(string token);

}