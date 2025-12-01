// api/Controllers/BoardController.cs

using api.Models.Board;
using api.Models.Requests;
using api.Services;
using dataccess.Entities;
using Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("[controller]")] // Base route: /Board/...
[Authorize]            // All endpoints here require an authenticated user
public class BoardController(IBoardService boardService) : ControllerBase
{
    /// <summary>
    /// Player buys a new board for the selected game.
    /// PlayerId is resolved from JWT inside the service.
    /// </summary>
    [HttpPost(nameof(CreateBoard))] // POST /Board/CreateBoard
    public async Task<BoardResponseDto> CreateBoard([FromBody] CreateBoardRequestDto dto)
    {
        var board = await boardService.CreateBoardForCurrentUser(User, dto);
        return MapToDto(board);
    }

    /// <summary>
    /// Admin overview: all boards for a specific game.
    /// Used to see who played which numbers and which boards are winning.
    /// </summary>
    [HttpGet(nameof(GetBoardsForGame))] // GET /Board/GetBoardsForGame?gameId=...
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<BoardResponseDto>> GetBoardsForGame([FromQuery] string gameId)
    {
        var boards = await boardService.GetBoardsForGame(gameId);
        return boards
            .Select(MapToDto)
            .ToList();
    }

    /// <summary>
    /// Admin overview: all boards for a specific player.
    /// This is useful as part of the player history / reporting.
    /// </summary>
    [HttpGet(nameof(GetBoardsForPlayer))] // GET /Board/GetBoardsForPlayer?playerId=...
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<BoardResponseDto>> GetBoardsForPlayer([FromQuery] string playerId)
    {
        var boards = await boardService.GetBoardsForPlayer(playerId);
        return boards
            .Select(MapToDto)
            .ToList();
    }

    /// <summary>
    /// Player: all boards belonging to the currently logged-in user.
    /// This is the main endpoint for the player "my boards / my history" view.
    /// </summary>
    [HttpGet(nameof(GetMyBoards))] // GET /Board/GetMyBoards
    public async Task<List<BoardResponseDto>> GetMyBoards()
    {
        var boards = await boardService.GetBoardsForCurrentUser(User);
        return boards
            .Select(MapToDto)
            .ToList();
    }

    /// <summary>
    /// Maps EF Board entity to a safe API response DTO.
    /// GameWeek / GameYear are populated from the navigation property (if loaded).
    /// </summary>
    private static BoardResponseDto MapToDto(Board b) => new()
    {
        Id           = b.Id,
        PlayerId     = b.Playerid ?? string.Empty,
        GameId       = b.Gameid ?? string.Empty,
        Numbers      = b.Numbers.ToArray(),
        Price        = b.Price,
        IsWinning    = b.Iswinning,
        RepeatWeeks  = b.Repeatweeks,
        RepeatActive = b.Repeatactive,
        CreatedAt    = b.Createdat,

        // Extra convenience for UI – game info comes from navigation property
        GameWeek = b.Game?.Weeknumber ?? 0,
        GameYear = b.Game?.Year       ?? 0
    };
}
