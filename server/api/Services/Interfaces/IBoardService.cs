// api/Services/IBoardService.cs
using System.Security.Claims;
using api.Models.Requests;
using dataccess.Entities;

namespace api.Services;

public interface IBoardService
{
    Task<Board> CreateBoard(string playerId, CreateBoardRequestDto dto);
    Task<List<Board>> GetBoardsForGame(string gameId);
    Task<List<Board>> GetBoardsForPlayer(string playerId);
    Task<Board> CreateBoardForCurrentUser(ClaimsPrincipal user, CreateBoardRequestDto dto);
    Task<List<Board>> GetBoardsForCurrentUser(ClaimsPrincipal user);
    Task<Board> StopRepeatingBoardForCurrentUser(ClaimsPrincipal user, string boardId);
}