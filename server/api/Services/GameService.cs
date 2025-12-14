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
        // Find the current active game (ignore soft-deleted)
        var game = await ctx.Games
            .Where(g => g.Deletedat == null && g.Isactive)
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Weeknumber)
            .FirstOrDefaultAsync();

        // If this happens, seeding or data is broken
        if (game == null)
        {
            throw new ValidationException("No active game found.");
        }

        return game;
    }

    public async Task<List<Game>> GetGamesHistory()
    {
        // All non-deleted games, newest first
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




    /// <summary>
    /// For all boards in the current game that are marked as repeating,
    /// try to create a new board in the next game and charge the weekly price again.
    ///
    /// Important:
    /// - RepeatWeeks means "how many FUTURE games this board should still participate in".
    ///   Example: RepeatWeeks = 3 => this board should auto-repeat for the next 3 games.
    /// - We only repeat boards where Repeatactive = true and Repeatweeks > 0.
    /// - For each repeat we:
    ///     * check the player's current balance (including extra charges planned in this loop),
    ///     * if enough balance -> create a new board with Price = weekly price,
    ///     * decrease remaining future weeks on the new board,
    ///     * if not enough balance -> stop repeating for this board (Repeatactive = false, Repeatweeks = 0).
    /// </summary>
    private async Task CreateRepeatingBoardsForNextGame(Game currentGame, Game nextGame)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Only non-deleted boards that are still set to repeat in future games
        var repeatingBoards = currentGame.Boards
            .Where(b =>
                b.Deletedat == null &&
                b.Repeatactive &&
                b.Repeatweeks > 0)
            .ToList();

        // Track additional "planned" charges per player so we do not overspend
        // when a player has multiple repeating boards in the same game.
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
                // Invalid board configuration, skip silently.
                continue;
            }

            // Current balance from DB
            var currentBalance = await GetCurrentBalance(playerId);

            // Subtract charges we already decided to apply during this loop
            extraChargesPerPlayer.TryGetValue(playerId, out var alreadyPlanned);
            var availableBalance = currentBalance - alreadyPlanned;

            if (availableBalance < weeklyPrice)
            {
                // Not enough money to repeat this board.
                // We also disable repeating so we do not try again next time.
                board.Repeatactive = false;
                board.Repeatweeks = 0;
                continue;
            }

            // There is enough balance to repeat this board for the next game.
            // We create a new board in the next game and charge one weekly price.
            var remainingFutureWeeks = board.Repeatweeks - 1;

            var newBoard = new Board
            {
                Id           = Guid.NewGuid().ToString(),
                Playerid     = playerId,
                Gameid       = nextGame.Id,
                Numbers      = board.Numbers.ToList(),

                // We always charge the weekly price per game.
                Price        = weeklyPrice,

                Iswinning    = false,

                // Remaining future weeks for auto-repeat after this newly created game
                Repeatweeks  = remainingFutureWeeks,
                Repeatactive = remainingFutureWeeks > 0,

                Createdat    = now,
                Deletedat    = null
            };

            ctx.Boards.Add(newBoard);

            // Remember that we have "spent" this weekly price from the player's balance
            extraChargesPerPlayer[playerId] = alreadyPlanned + weeklyPrice;
        }
    }

    public async Task<List<PlayerGameHistoryItemDto>> GetPlayerHistory(string playerEmail)
    {
        // Email must be present in the token
        if (string.IsNullOrWhiteSpace(playerEmail))
        {
            throw new ValidationException("Email claim is missing for the current user.");
        }

        // Find the player by email (ignore soft-deleted)
        var player = await ctx.Players
            .Where(p => p.Deletedat == null && p.Email == playerEmail)
            .FirstOrDefaultAsync();

        if (player == null)
        {
            // Domain-level error: logged in user is not registered as a player
            throw new ValidationException("Player not found for the current user.");
        }

        // Load all boards for this player, including related games
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

        // Map boards + games to a flat list of DTOs for the UI
        var history = boards
            .Select(b => new PlayerGameHistoryItemDto
            {
                GameId         = b.Gameid,
                WeekNumber     = b.Game!.Weeknumber,
                Year           = b.Game.Year,
                GameClosedAt   = b.Game.Closedat,

                BoardId        = b.Id,
                Numbers        = b.Numbers.ToArray(),

                // We show per-game price, and now Board.Price already stores weekly price.
                Price          = b.Price,
                BoardCreatedAt = b.Createdat,

                WinningNumbers = b.Game.Winningnumbers?.ToArray(),
                IsWinning      = b.Iswinning
            })
            .ToList();

        return history;
    }

    /// <summary>
    /// Helper for weekly board price based on count of numbers:
    /// 5 -> 20, 6 -> 40, 7 -> 80, 8 -> 160, otherwise 0.
    /// Must be consistent with BoardService.
    /// </summary>
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

    /// <summary>
    /// Calculates current balance for a player as:
    ///   sum(Approved transactions.amount) - sum(boards.Price).
    ///
    /// This is the same rule as in BoardService.
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
