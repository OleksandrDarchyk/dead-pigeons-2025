using api.Models.Requests;
using api.Services;
using dataccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
public class BoardController(IBoardService boardService) : ControllerBase
{
    // POST /CreateBoard
    // Player (through UI) buys a new board for a specific game
    [HttpPost(nameof(CreateBoard))]
    public async Task<Board> CreateBoard([FromBody] CreateBoardRequestDto dto)
    {
        // Later we can add [Authorize] attributes here
        var board = await boardService.CreateBoard(dto);
        return board;
    }

    // GET /GetBoardsForGame?gameId=...
    // Admin overview: see all boards for a specific game
    [HttpGet(nameof(GetBoardsForGame))]
    public async Task<List<Board>> GetBoardsForGame([FromQuery] string gameId)
    {
        var boards = await boardService.GetBoardsForGame(gameId);
        return boards;
    }

    // GET /GetBoardsForPlayer?playerId=...
    // Player history: all boards bought by a specific player
    [HttpGet(nameof(GetBoardsForPlayer))]
    public async Task<List<Board>> GetBoardsForPlayer([FromQuery] string playerId)
    {
        var boards = await boardService.GetBoardsForPlayer(playerId);
        return boards;
    }
}