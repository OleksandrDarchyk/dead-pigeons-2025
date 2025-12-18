using api.Models.Game;
using api.Models.Requests;
using dataccess.Entities;

namespace api.Services;

public interface IGameService
{ 
    Task<Game> GetActiveGame();
    Task<List<Game>> GetGamesHistory();
    
    Task<GameResultSummaryDto> SetWinningNumbers(SetWinningNumbersRequestDto dto);
    Task<List<PlayerGameHistoryItemDto>> GetPlayerHistory(string playerEmail);
}