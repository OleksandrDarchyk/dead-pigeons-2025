using api.Models.Game;
using api.Models.Requests;
using dataccess.Entities;

namespace api.Services;

public interface IGameService
{
    // Returns the currently active game (there should only be one active game at a time)
    Task<Game> GetActiveGame();

    // Returns the full history of games (both active and closed, but not soft-deleted)
    Task<List<Game>> GetGamesHistory();

    // Sets winning numbers for a game, marks winning boards, closes this game
    // and activates the next upcoming game (if any)
    Task<Game> SetWinningNumbers(SetWinningNumbersRequestDto dto);

    // Returns game history only for a specific player identified by email
    Task<List<PlayerGameHistoryItemDto>> GetPlayerHistory(string playerEmail);
}