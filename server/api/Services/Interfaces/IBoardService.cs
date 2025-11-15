using api.Models.Requests;
using dataccess.Entities;

namespace api.Services;

public interface IBoardService
{
    // Creates a new board (guess) for a player in a specific game
    Task<Board> CreateBoard(CreateBoardRequestDto dto);

    // Returns all boards for a given game (admin overview of that round)
    Task<List<Board>> GetBoardsForGame(string gameId);

    // Returns all boards for a given player (player history)
    Task<List<Board>> GetBoardsForPlayer(string playerId);
}