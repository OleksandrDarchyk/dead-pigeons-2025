// api/Services/BoardService.cs
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
// Domain validation errors should use Bogus.ValidationException
using ValidationException = Bogus.ValidationException;

namespace api.Services;

public class BoardService(
    MyDbContext ctx,
    TimeProvider timeProvider) : IBoardService
{
  
    public async Task<Board> CreateBoard(string playerId, CreateBoardRequestDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var numbers = dto.Numbers ?? Array.Empty<int>();

        if (numbers.Length is < 5 or > 8)
        {
            throw new ValidationException("Board must have between 5 and 8 numbers.");
        }

        if (numbers.Distinct().Count() != numbers.Length)
        {
            throw new ValidationException("Board numbers must be distinct.");
        }

        if (numbers.Any(n => n < 1 || n > 16))
        {
            throw new ValidationException("Board numbers must be between 1 and 16.");
        }

        if (dto.RepeatWeeks < 0)
        {
            throw new ValidationException("Repeat weeks cannot be negative.");
        }

        if (dto.RepeatWeeks > 52)
        {
            throw new ValidationException("Repeat weeks cannot be more than 52.");
        }

        var sortedNumbers = numbers.OrderBy(n => n).ToArray();

        var player = await ctx.Players
            .FirstOrDefaultAsync(p =>
                p.Id == playerId &&
                p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

        if (!player.Isactive)
        {
            throw new ValidationException("Only active players can buy boards.");
        }

        var game = await ctx.Games
            .FirstOrDefaultAsync(g =>
                g.Id == dto.GameId &&
                g.Deletedat == null);

        if (game == null)
        {
            throw new ValidationException("Game not found.");
        }

        if (!game.Isactive)
        {
            throw new ValidationException("Cannot buy boards for an inactive game.");
        }
        
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var dkZone = GetDanishTimeZone();
        var nowDk = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, dkZone);
        
        var weekStartDk = ISOWeek.ToDateTime(game.Year, game.Weeknumber, DayOfWeek.Monday);

        var saturdayDeadlineDk = weekStartDk
            .AddDays(5)         
            .AddHours(17);      

        if (nowDk >= saturdayDeadlineDk)
        {
            throw new ValidationException(
                "You can no longer join this week's game. " +
                "The deadline is Saturday at 17:00 Danish local time.");
        }

        var count = sortedNumbers.Length;
        var weeklyPrice = count switch
        {
            5 => 20,
            6 => 40,
            7 => 80,
            8 => 160,
            _ => throw new ValidationException("Board must have between 5 and 8 numbers.")
        };

        var totalCostForThisGame = weeklyPrice;

        var currentBalance = await GetCurrentBalance(playerId);

        if (currentBalance < totalCostForThisGame)
        {
            throw new ValidationException(
                "Not enough balance to buy this board for this game.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var board = new Board
        {
            Id           = Guid.NewGuid().ToString(),
            Playerid     = playerId,
            Gameid       = dto.GameId,
            Numbers      = sortedNumbers.ToList(),
            
            Price        = weeklyPrice,

            Iswinning    = false,
            Repeatweeks  = dto.RepeatWeeks,
            Repeatactive = dto.RepeatWeeks > 0,

            Createdat    = now,
            Deletedat    = null
        };

        ctx.Boards.Add(board);
        await ctx.SaveChangesAsync();

        return board;
    }
    
    public async Task<List<Board>> GetBoardsForGame(string gameId)
    {
        return await ctx.Boards
            .Where(b => b.Gameid == gameId && b.Deletedat == null)
            .Include(b => b.Player)
            .Include(b => b.Game)  
            .OrderBy(b => b.Createdat)
            .ToListAsync();
    }
    
    public async Task<List<Board>> GetBoardsForPlayer(string playerId)
    {
        return await ctx.Boards
            .Where(b => b.Playerid == playerId && b.Deletedat == null)
            .Include(b => b.Game) 
            .OrderByDescending(b => b.Createdat)
            .ToListAsync();
    }
    
    public async Task<Board> CreateBoardForCurrentUser(
        ClaimsPrincipal claims,
        CreateBoardRequestDto dto)
    {
        var email = claims.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email not found on current user.");
        }

        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Email == email && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found for current user.");
        }
        return await CreateBoard(player.Id, dto);
    }
    
    public async Task<List<Board>> GetBoardsForCurrentUser(ClaimsPrincipal claims)
    {
        var email = claims.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email not found on current user.");
        }

        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Email == email && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found for current user.");
        }

        return await GetBoardsForPlayer(player.Id);
    }
    
    public async Task<Board> StopRepeatingBoardForCurrentUser(ClaimsPrincipal claims, string boardId)
    {
        var email = claims.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email not found on current user.");
        }

        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Email == email && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found for current user.");
        }

        var board = await ctx.Boards
            .Include(b => b.Game)
            .FirstOrDefaultAsync(b => b.Id == boardId && b.Deletedat == null);

        if (board == null)
        {
            throw new ValidationException("Board not found.");
        }

        if (board.Playerid != player.Id)
        {
            throw new ValidationException("You can only stop repeating for your own boards.");
        }

        if (!board.Repeatactive && board.Repeatweeks == 0)
        {
            return board;
        }

        board.Repeatactive = false;
        board.Repeatweeks = 0;

        await ctx.SaveChangesAsync();

        return board;
    }
    
    private async Task<int> GetCurrentBalance(string playerId)
    {
        var approvedAmount = await ctx.Transactions
            .Where(t =>
                t.Playerid == playerId &&
                t.Deletedat == null &&
                t.Status == "Approved")
            .SumAsync(t => (int?)t.Amount) ?? 0;

        var spentOnBoards = await ctx.Boards
            .Where(b =>
                b.Playerid == playerId &&
                b.Deletedat == null)
            .SumAsync(b => (int?)b.Price) ?? 0;

        return approvedAmount - spentOnBoards;
    }
    
    private static TimeZoneInfo GetDanishTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Local;
            }
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }
}
