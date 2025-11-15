using api.Models.Requests;
using api.Services;
using dataccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
public class GamesController(IGameService gameService) : ControllerBase
{
    // GET /GetActiveGame
    // Returns the currently active game (there should be only one)
    [HttpGet(nameof(GetActiveGame))]
    public async Task<Game> GetActiveGame()
    {
        return await gameService.GetActiveGame();
    }

    // GET /GetGamesHistory
    // Returns the list of all games (history) for admin overview
    [HttpGet(nameof(GetGamesHistory))]
    public async Task<List<Game>> GetGamesHistory()
    {
        return await gameService.GetGamesHistory();
    }

    // POST /SetWinningNumbers
    // Admin closes the game by entering the 3 winning numbers
    [HttpPost(nameof(SetWinningNumbers))]
    public async Task<Game> SetWinningNumbers([FromBody] SetWinningNumbersRequestDto dto)
    {
        // Later we will add [Authorize(Roles = "Admin")] here
        return await gameService.SetWinningNumbers(dto);
    }
}