using api.Models;
using api.Models.Requests;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize] // by default, endpoints here require a valid JWT
public class AuthController(IAuthService authService) : ControllerBase
{
    // POST /Login – public endpoint: credentials in, JWT out
    [HttpPost(nameof(Login))]
    [AllowAnonymous]
    public async Task<JwtResponse> Login([FromBody] LoginRequestDto dto)
    {
        return await authService.Login(dto);
    }

    // POST /Register – create a new user and return a JWT
    [HttpPost(nameof(Register))]
    [AllowAnonymous]
    public async Task<JwtResponse> Register([FromBody] RegisterRequestDto dto)
    {
        return await authService.Register(dto);
    }

    // GET /WhoAmI – returns claims based on the current JWT
    [HttpGet(nameof(WhoAmI))]
    public JwtClaims WhoAmI()
    {
        return authService.GetCurrentUserClaims(User);
    }
}