using api.Models.Requests;
using api.Services;
using dataccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
public class PlayersController(IPlayerService playerService) : ControllerBase
{
    // POST /CreatePlayer
    [HttpPost(nameof(CreatePlayer))]
    public async Task<Player> CreatePlayer([FromBody] CreatePlayerRequestDto dto)
    {
        // Later we can add [Authorize(Roles = "Admin")]
        var player = await playerService.CreatePlayer(dto);
        return player;
    }

    // GET /GetPlayers?isActive=true/false
    [HttpGet(nameof(GetPlayers))]
    public async Task<List<Player>> GetPlayers([FromQuery] bool? isActive)
    {
        var players = await playerService.GetPlayers(isActive);
        return players;
    }

    // GET /GetPlayerById?playerId=...
    [HttpGet(nameof(GetPlayerById))]
    public async Task<Player> GetPlayerById([FromQuery] string playerId)
    {
        var player = await playerService.GetPlayerById(playerId);
        return player;
    }

    // POST /ActivatePlayer?playerId=...
    [HttpPost(nameof(ActivatePlayer))]
    public async Task<Player> ActivatePlayer([FromQuery] string playerId)
    {
        var player = await playerService.ActivatePlayer(playerId);
        return player;
    }

    // POST /DeactivatePlayer?playerId=...
    [HttpPost(nameof(DeactivatePlayer))]
    public async Task<Player> DeactivatePlayer([FromQuery] string playerId)
    {
        var player = await playerService.DeactivatePlayer(playerId);
        return player;
    }

    // POST /DeletePlayer?playerId=...
    [HttpPost(nameof(DeletePlayer))]
    public async Task<Player> DeletePlayer([FromQuery] string playerId)
    {
        var player = await playerService.SoftDeletePlayer(playerId);
        return player;
    }

    // POST /UpdatePlayer
    // Updates player basic information (name, email, phone)
    [HttpPost(nameof(UpdatePlayer))]
    public async Task<Player> UpdatePlayer([FromBody] UpdatePlayerRequestDto dto)
    {
        // Later we can add [Authorize(Roles = "Admin")]
        var player = await playerService.UpdatePlayer(dto);
        return player;
    }
}
