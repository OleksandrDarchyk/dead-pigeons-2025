using System.ComponentModel.DataAnnotations;
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
        // Validate DTO using DataAnnotations attributes
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var numbers = dto.Numbers ?? Array.Empty<int>();

        // Extra safety: must be between 5 and 8 numbers
        if (numbers.Length is < 5 or > 8)
        {
            throw new ValidationException("Board must have between 5 and 8 numbers.");
        }

        // Numbers must be unique
        if (numbers.Distinct().Count() != numbers.Length)
        {
            throw new ValidationException("Board numbers must be distinct.");
        }

        // Numbers must be in allowed range [1;16]
        if (numbers.Any(n => n < 1 || n > 16))
        {
            throw new ValidationException("Board numbers must be between 1 and 16.");
        }

        // Defensive validation for repeat weeks (even if DTO is already checked)
        if (dto.RepeatWeeks < 0)
        {
            throw new ValidationException("Repeat weeks cannot be negative.");
        }

        if (dto.RepeatWeeks > 52)
        {
            throw new ValidationException("Repeat weeks cannot be more than 52.");
        }

        var sortedNumbers = numbers.OrderBy(n => n).ToArray();

        // Ensure player exists and is not soft-deleted
        var player = await ctx.Players
            .FirstOrDefaultAsync(p =>
                p.Id == playerId &&
                p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

        // Only active players can participate in games
        if (!player.Isactive)
        {
            throw new ValidationException("Only active players can buy boards.");
        }

        // Ensure game exists and is not soft-deleted
        var game = await ctx.Games
            .FirstOrDefaultAsync(g =>
                g.Id == dto.GameId &&
                g.Deletedat == null);

        if (game == null)
        {
            throw new ValidationException("Game not found.");
        }

        // Business rule: players cannot buy boards for inactive games
        if (!game.Isactive)
        {
            throw new ValidationException("Cannot buy boards for an inactive game.");
        }

        // Price is calculated on the server based on number of fields
        var count = sortedNumbers.Length;
        var weeklyPrice = count switch
        {
            5 => 20,
            6 => 40,
            7 => 80,
            8 => 160,
            _ => throw new ValidationException("Board must have between 5 and 8 numbers.")
        };

        // How many weeks we must be able to cover with current balance:
        // 0 -> no repeat -> at least 1 week
        var weeksToCover = dto.RepeatWeeks <= 0 ? 1 : dto.RepeatWeeks;
        var totalCost = weeklyPrice * weeksToCover;

        // Balance = sum(approved transactions) - sum(board prices)
        var currentBalance = await GetCurrentBalance(playerId);

        if (currentBalance < totalCost)
        {
            throw new ValidationException(
                "Not enough balance to buy this board for the selected number of weeks.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var board = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = playerId,
            Gameid = dto.GameId,
            Numbers = sortedNumbers.ToList(),
            // We still store price for a single week; repeated rounds will be handled later
            Price = weeklyPrice,
            Iswinning = false,
            Repeatweeks = dto.RepeatWeeks,
            Repeatactive = dto.RepeatWeeks > 0,
            Createdat = now,
            Deletedat = null
        };

        ctx.Boards.Add(board);
        await ctx.SaveChangesAsync();

        return board;
    }

    public async Task<List<Board>> GetBoardsForGame(string gameId)
    {
        // All non-deleted boards for a specific game, with Player and Game included
        return await ctx.Boards
            .Where(b => b.Gameid == gameId && b.Deletedat == null)
            .Include(b => b.Player) // used for admin overview (player details)
            .Include(b => b.Game)   // used for GameWeek / GameYear in DTO
            .OrderBy(b => b.Createdat)
            .ToListAsync();
    }

    public async Task<List<Board>> GetBoardsForPlayer(string playerId)
    {
        // All non-deleted boards for a specific player (latest first), with Game included
        return await ctx.Boards
            .Where(b => b.Playerid == playerId && b.Deletedat == null)
            .Include(b => b.Game) // we can expose GameWeek / GameYear in DTO later
            .OrderByDescending(b => b.Createdat)
            .ToListAsync();
    }

    public async Task<Board> CreateBoardForCurrentUser(
        ClaimsPrincipal claims,
        CreateBoardRequestDto dto)
    {
        // Resolve player by email from JWT claims
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

        // Only the server decides which player owns the board
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

    /// <summary>
    /// Calculates current balance for a player as:
    /// sum(Approved transactions) - sum(board prices).
    /// </summary>
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
}
