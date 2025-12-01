using System.Security.Claims;
using api.Models.Game;
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
public class GamesController(IGameService gameService) : ControllerBase
{
    // GET /GetActiveGame
    // Returns the currently active game as a DTO
    [HttpGet(nameof(GetActiveGame))]
    public async Task<GameResponseDto> GetActiveGame()
    {
        var game = await gameService.GetActiveGame();
        return MapToDto(game);
    }

    // GET /GetGamesHistory
    // Admin overview of all games (past and current)
    [HttpGet(nameof(GetGamesHistory))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<GameResponseDto>> GetGamesHistory()
    {
        var games = await gameService.GetGamesHistory();

        return games
            .Select(MapToDto)
            .ToList();
    }

    // GET /GetMyGameHistory
    // Player-specific game history for the current logged-in user
    [HttpGet(nameof(GetMyGameHistory))]
    public async Task<List<PlayerGameHistoryItemDto>> GetMyGameHistory()
    {
        // Try to read email from standard ClaimTypes.Email first,
        // then fallback to a custom "email" claim if used.
        var email =
            User.FindFirst(ClaimTypes.Email)?.Value ??
            User.FindFirst("email")?.Value;

        var history = await gameService.GetPlayerHistory(email!);

        return history;
    }

    // POST /SetWinningNumbers
    // Only admin is allowed to close a game and set winners
    [HttpPost(nameof(SetWinningNumbers))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<GameResponseDto> SetWinningNumbers([FromBody] SetWinningNumbersRequestDto dto)
    {
        var game = await gameService.SetWinningNumbers(dto);
        return MapToDto(game);
    }

    // Maps EF entity to a safe API response DTO
    private static GameResponseDto MapToDto(Game g) => new()
    {
        Id = g.Id,
        WeekNumber = g.Weeknumber,
        Year = g.Year,
        WinningNumbers = g.Winningnumbers?.ToArray(),
        IsActive = g.Isactive,
        CreatedAt = g.Createdat,
        ClosedAt = g.Closedat
    };
}
