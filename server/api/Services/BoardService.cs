using System.ComponentModel.DataAnnotations;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class BoardService(
    MyDbContext ctx,
    TimeProvider timeProvider) : IBoardService
{
    public async Task<Board> CreateBoard(CreateBoardRequestDto dto)
    {
        // 1) Validate DTO attributes (DataAnnotations)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // 2) Validate numbers: length already checked by DTO (5–8),
        // but we still need distinct values and range 1–16
        var numbers = dto.Numbers;

        if (numbers.Distinct().Count() != numbers.Length)
        {
            throw new ValidationException("Board numbers must be distinct.");
        }

        if (numbers.Any(n => n < 1 || n > 16))
        {
            throw new ValidationException("Board numbers must be between 1 and 16.");
        }

        var sortedNumbers = numbers.OrderBy(n => n).ToArray();

        // 3) Ensure player exists, is not soft-deleted and is active
        var player = await ctx.Players
            .FirstOrDefaultAsync(p =>
                p.Id == dto.PlayerId &&
                p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

        if (!player.Isactive)
        {
            throw new ValidationException("Only active players can buy boards.");
        }

        // 4) Ensure game exists, is not soft-deleted and is active
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

        // 5) Calculate price based on how many numbers the board has
        var count = sortedNumbers.Length;
        var price = count switch
        {
            5 => 20,
            6 => 40,
            7 => 80,
            8 => 160,
            _ => throw new ValidationException("Board must have between 5 and 8 numbers.")
        };

        // 6) Calculate current balance:
        // balance = sum(Approved transactions) - sum(board prices)
        var approvedAmount = await ctx.Transactions
            .Where(t =>
                t.Playerid == dto.PlayerId &&
                t.Deletedat == null &&
                t.Status == "Approved")
            .SumAsync(t => (int?)t.Amount) ?? 0;

        var spentOnBoards = await ctx.Boards
            .Where(b =>
                b.Playerid == dto.PlayerId &&
                b.Deletedat == null)
            .SumAsync(b => (int?)b.Price) ?? 0;

        var currentBalance = approvedAmount - spentOnBoards;

        if (currentBalance < price)
        {
            throw new ValidationException("Not enough balance to buy this board.");
        }

        // 7) Create the new board entity
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var board = new Board
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = dto.PlayerId,
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
        // Return all non-deleted boards for this game
        return await ctx.Boards
            .Where(b => b.Gameid == gameId && b.Deletedat == null)
            .OrderBy(b => b.Createdat)
            .ToListAsync();
    }

    public async Task<List<Board>> GetBoardsForPlayer(string playerId)
    {
        // Return all non-deleted boards for this player (latest first)
        return await ctx.Boards
            .Where(b => b.Playerid == playerId && b.Deletedat == null)
            .OrderByDescending(b => b.Createdat)
            .ToListAsync();
    }
}
