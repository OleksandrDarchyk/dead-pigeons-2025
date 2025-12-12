// api/Services/IBoardService.cs
using System.Security.Claims;
using api.Models.Requests;
using dataccess.Entities;

namespace api.Services;

public interface IBoardService
{
    // Create a board for a specific player (used from helpers or admin endpoints)
    Task<Board> CreateBoard(string playerId, CreateBoardRequestDto dto);

    // Boards for a specific game (admin view)
    Task<List<Board>> GetBoardsForGame(string gameId);

    // Boards for a specific player (admin view)
    Task<List<Board>> GetBoardsForPlayer(string playerId);

    // Use the current logged-in user as the player
    Task<Board> CreateBoardForCurrentUser(ClaimsPrincipal user, CreateBoardRequestDto dto);

    // Boards belonging to the current logged-in user
    Task<List<Board>> GetBoardsForCurrentUser(ClaimsPrincipal user);

    // Stop repeating a board that belongs to the current logged-in user
    Task<Board> StopRepeatingBoardForCurrentUser(ClaimsPrincipal user, string boardId);
}