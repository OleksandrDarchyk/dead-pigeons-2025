using System.ComponentModel.DataAnnotations;
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

    public async Task<Game> SetWinningNumbers(SetWinningNumbersRequestDto dto)
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

        // Business rule: only the currently active game can be closed
        if (!game.Isactive)
        {
            throw new ValidationException("Only the active game can be closed and assigned winning numbers.");
        }

        // Do not allow closing the same game twice
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

        // Save all changes in one go
        await ctx.SaveChangesAsync();

        return game;
    }
}
