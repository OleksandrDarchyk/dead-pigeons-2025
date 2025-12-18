using api.Models.Requests;
using api.Models.Responses;
using api.Services;
using dataccess.Entities;
using Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
public class PlayersController(IPlayerService playerService) : ControllerBase
{
  
    [HttpPost(nameof(CreatePlayer))]
    public async Task<PlayerResponseDto> CreatePlayer([FromBody] CreatePlayerRequestDto dto)
    {
        var player = await playerService.CreatePlayer(dto);
        return MapToDto(player);
    }
    
    [HttpGet(nameof(GetPlayers))]
    public async Task<List<PlayerResponseDto>> GetPlayers(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? direction = null)
    {
        var players = await playerService.GetPlayers(isActive, sortBy, direction);

        return players
            .Select(MapToDto)
            .ToList();
    }
    
    [HttpGet(nameof(GetPlayerById))]
    public async Task<PlayerResponseDto> GetPlayerById([FromQuery] string playerId)
    {
        var player = await playerService.GetPlayerById(playerId);
        return MapToDto(player);
    }
    
    [HttpPost(nameof(ActivatePlayer))]
    public async Task<PlayerResponseDto> ActivatePlayer([FromQuery] string playerId)
    {
        var player = await playerService.ActivatePlayer(playerId);
        return MapToDto(player);
    }
    
    [HttpPost(nameof(DeactivatePlayer))]
    public async Task<PlayerResponseDto> DeactivatePlayer([FromQuery] string playerId)
    {
        var player = await playerService.DeactivatePlayer(playerId);
        return MapToDto(player);
    }
    
    [HttpPost(nameof(DeletePlayer))]
    public async Task<PlayerResponseDto> DeletePlayer([FromQuery] string playerId)
    {
        var player = await playerService.SoftDeletePlayer(playerId);
        return MapToDto(player);
    }
    
    [HttpPost(nameof(UpdatePlayer))]
    public async Task<PlayerResponseDto> UpdatePlayer([FromBody] UpdatePlayerRequestDto dto)
    {
        var player = await playerService.UpdatePlayer(dto);
        return MapToDto(player);
    }
    private static PlayerResponseDto MapToDto(Player p) => new()
    {
        Id = p.Id,
        FullName = p.Fullname,
        Email = p.Email,
        Phone = p.Phone,
        IsActive = p.Isactive,
        ActivatedAt = p.Activatedat,
        CreatedAt = p.Createdat
    };
}
