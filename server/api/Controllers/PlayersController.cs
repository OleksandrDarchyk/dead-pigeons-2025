// api/Controllers/PlayersController.cs
using api.Models.Requests;
using api.Models.Responses;
using api.Services;
using dataccess.Entities;
using Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
// All endpoints in this controller are for admins only
[Authorize(Roles = Roles.Admin)]
public class PlayersController(IPlayerService playerService) : ControllerBase
{
    // Admin: create a new player
    // POST /CreatePlayer
    [HttpPost(nameof(CreatePlayer))]
    public async Task<PlayerResponseDto> CreatePlayer([FromBody] CreatePlayerRequestDto dto)
    {
        // Service still returns the EF entity, controller maps it to DTO
        var player = await playerService.CreatePlayer(dto);
        return MapToDto(player);
    }

    // Admin: list players, optionally filtered by active flag
    // GET /GetPlayers?isActive=true/false
    [HttpGet(nameof(GetPlayers))]
    public async Task<List<PlayerResponseDto>> GetPlayers([FromQuery] bool? isActive = null)
    {
        var players = await playerService.GetPlayers(isActive);
        return players
            .Select(MapToDto)
            .ToList();
    }

    // Admin: get a single player by id
    // GET /GetPlayerById?playerId=...
    [HttpGet(nameof(GetPlayerById))]
    public async Task<PlayerResponseDto> GetPlayerById([FromQuery] string playerId)
    {
        var player = await playerService.GetPlayerById(playerId);
        return MapToDto(player);
    }

    // Admin: activate a player
    // POST /ActivatePlayer?playerId=...
    [HttpPost(nameof(ActivatePlayer))]
    public async Task<PlayerResponseDto> ActivatePlayer([FromQuery] string playerId)
    {
        var player = await playerService.ActivatePlayer(playerId);
        return MapToDto(player);
    }

    // Admin: deactivate a player
    // POST /DeactivatePlayer?playerId=...
    [HttpPost(nameof(DeactivatePlayer))]
    public async Task<PlayerResponseDto> DeactivatePlayer([FromQuery] string playerId)
    {
        var player = await playerService.DeactivatePlayer(playerId);
        return MapToDto(player);
    }

    // Admin: soft delete a player
    // POST /DeletePlayer?playerId=...
    [HttpPost(nameof(DeletePlayer))]
    public async Task<PlayerResponseDto> DeletePlayer([FromQuery] string playerId)
    {
        var player = await playerService.SoftDeletePlayer(playerId);
        return MapToDto(player);
    }

    // Admin: update player basic data
    // POST /UpdatePlayer
    [HttpPost(nameof(UpdatePlayer))]
    public async Task<PlayerResponseDto> UpdatePlayer([FromBody] UpdatePlayerRequestDto dto)
    {
        var player = await playerService.UpdatePlayer(dto);
        return MapToDto(player);
    }

    // Maps EF Player entity to a safe API response DTO
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
