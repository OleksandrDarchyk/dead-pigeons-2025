using System.Security.Claims;
using api.Models.Game;
using api.Models.Requests;
using api.Models.Responses;
using api.Services;
using Api.Security;
using dataccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize] 
public class GamesController(IGameService gameService) : ControllerBase
{
    [HttpGet(nameof(GetActiveGame))]
    public async Task<GameResponseDto> GetActiveGame()
    {
        var game = await gameService.GetActiveGame();
        return MapToDto(game);
    }
    
    [HttpGet(nameof(GetGamesHistory))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<GameResponseDto>> GetGamesHistory()
    {
        var games = await gameService.GetGamesHistory();

        return games
            .Select(MapToDto)
            .ToList();
    }
    
    [HttpGet(nameof(GetMyGameHistory))]
    public async Task<List<PlayerGameHistoryItemDto>> GetMyGameHistory()
    {
        var email =
            User.FindFirst(ClaimTypes.Email)?.Value ??
            User.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Email claim is missing for the current user.");
        }

        var history = await gameService.GetPlayerHistory(email);
        return history;
    }
    
    [HttpPost(nameof(SetWinningNumbers))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<GameResultSummaryDto> SetWinningNumbers([FromBody] SetWinningNumbersRequestDto dto)
    {
        var summary = await gameService.SetWinningNumbers(dto);

        return summary;
    }
    
    private static GameResponseDto MapToDto(Game g) => new()
    {
        Id            = g.Id,
        WeekNumber    = g.Weeknumber,
        Year          = g.Year,
        WinningNumbers = g.Winningnumbers?.ToArray(),
        IsActive      = g.Isactive,
        CreatedAt     = g.Createdat,
        ClosedAt      = g.Closedat
    };
}
