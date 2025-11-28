// api/Controllers/BoardController.cs

using api.Models.Board;
using api.Models.Requests;
using api.Models.Responses;
using api.Services;
using dataccess.Entities;
using Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize] // all endpoints here require an authenticated user
public class BoardController(IBoardService boardService) : ControllerBase
{
    // Player buys a new board for the current game
    // PlayerId is resolved from JWT inside the service
    [HttpPost(nameof(CreateBoard))]
    public async Task<BoardResponseDto> CreateBoard([FromBody] CreateBoardRequestDto dto)
    {
        var board = await boardService.CreateBoardForCurrentUser(User, dto);
        return MapToDto(board);
    }

    // Admin overview: all boards for a specific game
    [HttpGet(nameof(GetBoardsForGame))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<BoardResponseDto>> GetBoardsForGame([FromQuery] string gameId)
    {
        var boards = await boardService.GetBoardsForGame(gameId);
        return boards
            .Select(MapToDto)
            .ToList();
    }

    // Admin: all boards for a specific player
    [HttpGet(nameof(GetBoardsForPlayer))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<BoardResponseDto>> GetBoardsForPlayer([FromQuery] string playerId)
    {
        var boards = await boardService.GetBoardsForPlayer(playerId);
        return boards
            .Select(MapToDto)
            .ToList();
    }

    // Player: only boards for the current logged-in user
    [HttpGet(nameof(GetMyBoards))]
    public async Task<List<BoardResponseDto>> GetMyBoards()
    {
        var boards = await boardService.GetBoardsForCurrentUser(User);
        return boards
            .Select(MapToDto)
            .ToList();
    }

    // Maps EF entity to a safe API response DTO
    private static BoardResponseDto MapToDto(Board b) => new()
    {
        Id = b.Id,
        PlayerId = b.Playerid ?? string.Empty,
        GameId = b.Gameid ?? string.Empty,
        Numbers = b.Numbers.ToArray(),
        Price = b.Price,
        IsWinning = b.Iswinning,
        RepeatWeeks = b.Repeatweeks,
        RepeatActive = b.Repeatactive,
        CreatedAt = b.Createdat,

        // Extra convenience for UI – game info comes from navigation property
        GameWeek = b.Game?.Weeknumber ?? 0,
        GameYear = b.Game?.Year ?? 0
    };
}
