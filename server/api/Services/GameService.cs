// api/Services/GameService.cs
using System.ComponentModel.DataAnnotations;
using System.Data;
using api.Models.Game;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
// Domain validation errors should use Bogus.ValidationException
using ValidationException = Bogus.ValidationException;

namespace api.Services;

public class GameService(
    MyDbContext ctx,
    TimeProvider timeProvider) : IGameService
{
    public async Task<Game> GetActiveGame()
    {
        var game = await ctx.Games
            .Where(g => g.Deletedat == null && g.Isactive)
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Weeknumber)
            .FirstOrDefaultAsync();

        if (game == null)
        {
            throw new ValidationException("No active game found.");
        }

        return game;
    }

    public async Task<List<Game>> GetGamesHistory()
    {
        return await ctx.Games
            .Where(g => g.Deletedat == null)
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Weeknumber)
            .ToListAsync();
    }
    
public async Task<GameResultSummaryDto> SetWinningNumbers(SetWinningNumbersRequestDto dto)
{
    Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

    var numbers = dto.WinningNumbers;

    if (numbers.Distinct().Count() != 3)
    {
        throw new ValidationException("Winning numbers must be 3 distinct values.");
    }

    if (numbers.Any(n => n < 1 || n > 16))
    {
        throw new ValidationException("Winning numbers must be between 1 and 16.");
    }

    var sortedNumbers = numbers.OrderBy(n => n).ToArray();

    var alreadyInTransaction = ctx.Database.CurrentTransaction != null;

    await using var tx = alreadyInTransaction
        ? null
        : await ctx.Database.BeginTransactionAsync(IsolationLevel.Serializable);

    try
    {
        var game = await ctx.Games
            .Include(g => g.Boards)
            .FirstOrDefaultAsync(g => g.Id == dto.GameId && g.Deletedat == null);

        if (game == null)
        {
            throw new ValidationException("Game not found.");
        }

        if (!game.Isactive)
        {
            throw new ValidationException("Game already finished.");
        }

        if (game.Winningnumbers != null)
        {
            throw new ValidationException("Winning numbers already set for this game.");
        }

        var nextGame = await ctx.Games
            .Where(g =>
                g.Deletedat == null &&
                !g.Isactive &&
                (g.Year > game.Year ||
                 (g.Year == game.Year && g.Weeknumber > game.Weeknumber)))
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Weeknumber)
            .FirstOrDefaultAsync();

        if (nextGame == null)
        {
            throw new ValidationException("Next game not found. Seeding is missing or data is corrupted.");
        }

        game.Winningnumbers = sortedNumbers.ToList();
        game.Closedat = timeProvider.GetUtcNow().UtcDateTime;
        game.Isactive = false;

        var winningSet = sortedNumbers.ToHashSet();

        foreach (var board in game.Boards.Where(b => b.Deletedat == null))
        {
            var boardSet = board.Numbers.ToHashSet();
            board.Iswinning = winningSet.All(n => boardSet.Contains(n));
        }

        nextGame.Isactive = true;

        await CreateRepeatingBoardsForNextGame(game, nextGame);

        var totalBoards = game.Boards.Count(b => b.Deletedat == null);
        var winningBoards = game.Boards.Count(b => b.Deletedat == null && b.Iswinning);
        var digitalRevenue = game.Boards
            .Where(b => b.Deletedat == null)
            .Sum(b => b.Price);

        await ctx.SaveChangesAsync();

        if (tx != null)
        {
            await tx.CommitAsync();
        }

        return new GameResultSummaryDto
        {
            GameId = game.Id,
            WeekNumber = game.Weeknumber,
            Year = game.Year,
            WinningNumbers = sortedNumbers,
            TotalBoards = totalBoards,
            WinningBoards = winningBoards,
            DigitalRevenue = digitalRevenue
        };
    }
    catch
    {
        if (tx != null)
        {
            await tx.RollbackAsync();
        }

        throw;
    }
}

    private async Task CreateRepeatingBoardsForNextGame(Game currentGame, Game nextGame)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var repeatingBoards = currentGame.Boards
            .Where(b =>
                b.Deletedat == null &&
                b.Repeatactive &&
                b.Repeatweeks > 0)
            .ToList();
        
        var extraChargesPerPlayer = new Dictionary<string, int>();

        foreach (var board in repeatingBoards)
        {
            var playerId = board.Playerid;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                continue;
            }

            var weeklyPrice = CalculateWeeklyPrice(board);
            if (weeklyPrice <= 0)
            {
                continue;
            }

            var currentBalance = await GetCurrentBalance(playerId);

            extraChargesPerPlayer.TryGetValue(playerId, out var alreadyPlanned);
            var availableBalance = currentBalance - alreadyPlanned;

            if (availableBalance < weeklyPrice)
            {
                board.Repeatactive = false;
                board.Repeatweeks = 0;
                continue;
            }
            
            var remainingFutureWeeks = board.Repeatweeks - 1;

            var newBoard = new Board
            {
                Id           = Guid.NewGuid().ToString(),
                Playerid     = playerId,
                Gameid       = nextGame.Id,
                Numbers      = board.Numbers.ToList(),

                Price        = weeklyPrice,

                Iswinning    = false,

                Repeatweeks  = remainingFutureWeeks,
                Repeatactive = remainingFutureWeeks > 0,

                Createdat    = now,
                Deletedat    = null
            };

            ctx.Boards.Add(newBoard);
            extraChargesPerPlayer[playerId] = alreadyPlanned + weeklyPrice;
        }
    }

    public async Task<List<PlayerGameHistoryItemDto>> GetPlayerHistory(string playerEmail)
    {
        if (string.IsNullOrWhiteSpace(playerEmail))
        {
            throw new ValidationException("Email claim is missing for the current user.");
        }

        var player = await ctx.Players
            .Where(p => p.Deletedat == null && p.Email == playerEmail)
            .FirstOrDefaultAsync();

        if (player == null)
        {
            throw new ValidationException("Player not found for the current user.");
        }

        var boards = await ctx.Boards
            .Include(b => b.Game)
            .Where(b =>
                b.Deletedat == null &&
                b.Playerid == player.Id &&
                b.Game != null &&
                b.Game.Deletedat == null)
            .OrderByDescending(b => b.Game!.Year)
            .ThenByDescending(b => b.Game!.Weeknumber)
            .ToListAsync();

        var history = boards
            .Select(b => new PlayerGameHistoryItemDto
            {
                GameId         = b.Gameid,
                WeekNumber     = b.Game!.Weeknumber,
                Year           = b.Game.Year,
                GameClosedAt   = b.Game.Closedat,

                BoardId        = b.Id,
                Numbers        = b.Numbers.ToArray(),

                Price          = b.Price,
                BoardCreatedAt = b.Createdat,

                WinningNumbers = b.Game.Winningnumbers?.ToArray(),
                IsWinning      = b.Iswinning
            })
            .ToList();

        return history;
    }
    
    private static int CalculateWeeklyPrice(Board board)
    {
        var count = board.Numbers?.Count ?? 0;

        return count switch
        {
            5 => 20,
            6 => 40,
            7 => 80,
            8 => 160,
            _ => 0
        };
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
}
