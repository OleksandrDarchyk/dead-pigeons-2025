using System.ComponentModel.DataAnnotations;
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
        // Validate DTO via DataAnnotations attributes
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var numbers = dto.WinningNumbers;

        // Must be exactly 3 distinct numbers
        if (numbers.Distinct().Count() != 3)
        {
            throw new ValidationException("Winning numbers must be 3 distinct values.");
        }

        // Each number must be in [1;16]
        if (numbers.Any(n => n < 1 || n > 16))
        {
            throw new ValidationException("Winning numbers must be between 1 and 16.");
        }

        // We always sort them so order never matters
        var sortedNumbers = numbers.OrderBy(n => n).ToArray();

        // Load the game with its boards (only if not soft-deleted)
        var game = await ctx.Games
            .Include(g => g.Boards)
            .FirstOrDefaultAsync(g => g.Id == dto.GameId && g.Deletedat == null);

        if (game == null)
        {
            throw new ValidationException("Game not found.");
        }

        // Protect from double-submit: if the game is not active, then it is already finished
        if (!game.Isactive)
        {
            throw new ValidationException("Game already finished.");
        }

        // Extra guard: if for some reason game is still active, but winning numbers already exist
        if (game.Winningnumbers != null)
        {
            throw new ValidationException("Winning numbers already set for this game.");
        }

        // Close current game and set winning numbers
        game.Winningnumbers = sortedNumbers.ToList();
        game.Closedat = timeProvider.GetUtcNow().UtcDateTime;
        game.Isactive = false;

        // Mark winning boards:
        // a board wins if it contains all 3 winning numbers
        var winningSet = sortedNumbers.ToHashSet();

        foreach (var board in game.Boards.Where(b => b.Deletedat == null))
        {
            var boardSet = board.Numbers.ToHashSet();
            var isWinning = winningSet.All(n => boardSet.Contains(n));
            board.Iswinning = isWinning;
        }

        // Activate the next upcoming game by (year, weekNumber)
        var nextGame = await ctx.Games
            .Where(g =>
                g.Deletedat == null &&
                !g.Isactive &&
                (g.Year > game.Year ||
                 (g.Year == game.Year && g.Weeknumber > game.Weeknumber)))
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Weeknumber)
            .FirstOrDefaultAsync();

        if (nextGame != null)
        {
            nextGame.Isactive = true;
        }

        // Calculate summary for the just-closed game
        var totalBoards = game.Boards
            .Count(b => b.Deletedat == null);

        var winningBoards = game.Boards
            .Count(b => b.Deletedat == null && b.Iswinning);

        var digitalRevenue = game.Boards
            .Where(b => b.Deletedat == null)
            .Sum(b => b.Price);

        // Save all changes in one go
        await ctx.SaveChangesAsync();

        // Return a summary DTO for the UI
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
                GameId = b.Gameid,
                WeekNumber = b.Game!.Weeknumber,
                Year = b.Game.Year,
                GameClosedAt = b.Game.Closedat,

                BoardId = b.Id,
                Numbers = b.Numbers.ToArray(),
                Price = b.Price,
                BoardCreatedAt = b.Createdat,

                WinningNumbers = b.Game.Winningnumbers?.ToArray(),
                IsWinning = b.Iswinning
            })
            .ToList();

        return history;
    }
}
