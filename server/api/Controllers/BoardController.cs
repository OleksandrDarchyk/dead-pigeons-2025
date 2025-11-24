// api/Controllers/BoardController.cs
using api.Models.Requests;
using api.Services;
using dataccess.Entities;
using Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize]
public class BoardController(IBoardService boardService) : ControllerBase
{
    // Player buys a new board for the current game
    // PlayerId is resolved from JWT inside the service
    [HttpPost(nameof(CreateBoard))]
    public async Task<Board> CreateBoard([FromBody] CreateBoardRequestDto dto)
    {
        return await boardService.CreateBoardForCurrentUser(User, dto);
    }

    // Admin overview: all boards for a specific game
    [HttpGet(nameof(GetBoardsForGame))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<Board>> GetBoardsForGame([FromQuery] string gameId)
    {
        return await boardService.GetBoardsForGame(gameId);
    }

    // Admin: all boards for a specific player
    [HttpGet(nameof(GetBoardsForPlayer))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<Board>> GetBoardsForPlayer([FromQuery] string playerId)
    {
        return await boardService.GetBoardsForPlayer(playerId);
    }

    // Player: only boards for the current logged-in user
    [HttpGet(nameof(GetMyBoards))]
    public async Task<List<Board>> GetMyBoards()
    {
        return await boardService.GetBoardsForCurrentUser(User);
    }
}