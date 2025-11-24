using api.Models.Requests;
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
    [HttpGet(nameof(GetActiveGame))]
    public async Task<Game> GetActiveGame()
    {
        return await gameService.GetActiveGame();
    }

    // GET /GetGamesHistory
    // Admin overview of all games (past and current)
    [HttpGet(nameof(GetGamesHistory))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<Game>> GetGamesHistory()
    {
        return await gameService.GetGamesHistory();
    }

    // POST /SetWinningNumbers
    // Only admin is allowed to close a game and set winners
    [HttpPost(nameof(SetWinningNumbers))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<Game> SetWinningNumbers([FromBody] SetWinningNumbersRequestDto dto)
    {
        return await gameService.SetWinningNumbers(dto);
    }
}