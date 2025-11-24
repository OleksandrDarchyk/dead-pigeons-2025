// api/Controllers/PlayersController.cs
using api.Models.Requests;
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
    public async Task<Player> CreatePlayer([FromBody] CreatePlayerRequestDto dto)
    {
        return await playerService.CreatePlayer(dto);
    }

    // Admin: list players, optionally filtered by active flag
    // GET /GetPlayers?isActive=true/false
    [HttpGet(nameof(GetPlayers))]
    public async Task<List<Player>> GetPlayers([FromQuery] bool? isActive = null)
    {
        return await playerService.GetPlayers(isActive);
    }

    // Admin: get a single player by id
    // GET /GetPlayerById?playerId=...
    [HttpGet(nameof(GetPlayerById))]
    public async Task<Player> GetPlayerById([FromQuery] string playerId)
    {
        return await playerService.GetPlayerById(playerId);
    }

    // Admin: activate a player
    // POST /ActivatePlayer?playerId=...
    [HttpPost(nameof(ActivatePlayer))]
    public async Task<Player> ActivatePlayer([FromQuery] string playerId)
    {
        return await playerService.ActivatePlayer(playerId);
    }

    // Admin: deactivate a player
    // POST /DeactivatePlayer?playerId=...
    [HttpPost(nameof(DeactivatePlayer))]
    public async Task<Player> DeactivatePlayer([FromQuery] string playerId)
    {
        return await playerService.DeactivatePlayer(playerId);
    }

    // Admin: soft delete a player
    // POST /DeletePlayer?playerId=...
    [HttpPost(nameof(DeletePlayer))]
    public async Task<Player> DeletePlayer([FromQuery] string playerId)
    {
        return await playerService.SoftDeletePlayer(playerId);
    }

    // Admin: update player basic data
    // POST /UpdatePlayer
    [HttpPost(nameof(UpdatePlayer))]
    public async Task<Player> UpdatePlayer([FromBody] UpdatePlayerRequestDto dto)
    {
        return await playerService.UpdatePlayer(dto);
    }
}
