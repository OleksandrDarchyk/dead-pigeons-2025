using api.Models.Board;
using api.Models.Requests;
using api.Services;
using Api.Security;
using dataccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("[controller]")] 
[Authorize]         
public class BoardController(IBoardService boardService) : ControllerBase
{
    [HttpPost(nameof(CreateBoard))] 
    public async Task<BoardResponseDto> CreateBoard([FromBody] CreateBoardRequestDto dto)
    {
        var board = await boardService.CreateBoardForCurrentUser(User, dto);
        return MapToDto(board);
    }
    
    [HttpPost(nameof(StopRepeatingMyBoard))] 
    public async Task<BoardResponseDto> StopRepeatingMyBoard([FromBody] StopRepeatingBoardRequestDto dto)
    {
        var board = await boardService.StopRepeatingBoardForCurrentUser(User, dto.BoardId);
        return MapToDto(board);
    }
    
    [HttpGet(nameof(GetBoardsForGame))] 
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<BoardResponseDto>> GetBoardsForGame([FromQuery] string gameId)
    {
        var boards = await boardService.GetBoardsForGame(gameId);
        return boards
            .Select(MapToDto)
            .ToList();
    }
    
    [HttpGet(nameof(GetBoardsForPlayer))] 
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<BoardResponseDto>> GetBoardsForPlayer([FromQuery] string playerId)
    {
        var boards = await boardService.GetBoardsForPlayer(playerId);
        return boards
            .Select(MapToDto)
            .ToList();
    }
    
    [HttpGet(nameof(GetMyBoards))] 
    public async Task<List<BoardResponseDto>> GetMyBoards()
    {
        var boards = await boardService.GetBoardsForCurrentUser(User);
        return boards
            .Select(MapToDto)
            .ToList();
    }
    
    private static BoardResponseDto MapToDto(Board b) => new()
    {
        Id           = b.Id,
        PlayerId     = b.Playerid ?? string.Empty,
        GameId       = b.Gameid   ?? string.Empty,
        Numbers      = b.Numbers.ToArray(),
        Price        = b.Price,
        IsWinning    = b.Iswinning,
        RepeatWeeks  = b.Repeatweeks,
        RepeatActive = b.Repeatactive,
        CreatedAt    = b.Createdat,
        
        GameWeek     = b.Game?.Weeknumber ?? 0,
        GameYear     = b.Game?.Year       ?? 0,
        GameIsActive = b.Game?.Isactive   ?? false,
        GameClosedAt = b.Game?.Closedat
    };
}
