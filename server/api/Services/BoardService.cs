using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = Bogus.ValidationException;

namespace api.Services;

public class BoardService(
    MyDbContext ctx,
    TimeProvider timeProvider) : IBoardService
{
    public async Task<Board> CreateBoard(string playerId, CreateBoardRequestDto dto)
    {
        // Validate DTO via DataAnnotations
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var numbers = dto.Numbers ?? Array.Empty<int>();

        // Extra safety: length 5–8
        if (numbers.Length is < 5 or > 8)
        {
            throw new ValidationException("Board must have between 5 and 8 numbers.");
        }

        // Numbers must be unique
        if (numbers.Distinct().Count() != numbers.Length)
        {
            throw new ValidationException("Board numbers must be distinct.");
        }

        // Numbers must be within 1–16
        if (numbers.Any(n => n < 1 || n > 16))
        {
            throw new ValidationException("Board numbers must be between 1 and 16.");
        }

        var sortedNumbers = numbers.OrderBy(n => n).ToArray();

        // Ensure player exists, not soft-deleted
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

        // Ensure game exists, not soft-deleted
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

        // Price depends on numbers count
        var count = sortedNumbers.Length;
        var price = count switch
        {
            5 => 20,
            6 => 40,
            7 => 80,
            8 => 160,
            _ => throw new ValidationException("Board must have between 5 and 8 numbers.")
        };

        // balance = sum(Approved transactions) - sum(board prices)
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

        var currentBalance = approvedAmount - spentOnBoards;

        if (currentBalance < price)
        {
            throw new ValidationException("Not enough balance to buy this board.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var board = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = playerId,
            Gameid = dto.GameId,
            Numbers = sortedNumbers.ToList(),
            Price = price,
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
        // All non-deleted boards for a specific game, with Player included
        return await ctx.Boards
            .Where(b => b.Gameid == gameId && b.Deletedat == null)
            .Include(b => b.Player)
            .OrderBy(b => b.Createdat)
            .ToListAsync();
    }

    public async Task<List<Board>> GetBoardsForPlayer(string playerId)
    {
        // All non-deleted boards for a specific player (latest first)
        return await ctx.Boards
            .Where(b => b.Playerid == playerId && b.Deletedat == null)
            .OrderByDescending(b => b.Createdat)
            .ToListAsync();
    }

    public async Task<Board> CreateBoardForCurrentUser(
        ClaimsPrincipal claims,
        CreateBoardRequestDto dto)
    {
        // Use email from JWT to resolve the player
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
}
