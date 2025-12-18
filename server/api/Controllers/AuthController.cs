using api.Models;
using api.Models.Requests;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize] 
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost(nameof(Login))]
    [AllowAnonymous]
    public async Task<JwtResponse> Login([FromBody] LoginRequestDto dto)
    {
        return await authService.Login(dto);
    }

    [HttpPost(nameof(Register))]
    [AllowAnonymous]
    public async Task<JwtResponse> Register([FromBody] RegisterRequestDto dto)
    {
        return await authService.Register(dto);
    }

    [HttpGet(nameof(WhoAmI))]
    public JwtClaims WhoAmI()
    {
        return authService.GetCurrentUserClaims(User);
    }
}