using System.ComponentModel.DataAnnotations;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class GameService(
    MyDbContext ctx,
    TimeProvider timeProvider) : IGameService
{
    public async Task<Game> GetActiveGame()
    {
        // Find the currently active game (not soft-deleted)
        var game = await ctx.Games
            .Where(g => g.Deletedat == null && g.Isactive)
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Weeknumber)
            .FirstOrDefaultAsync();

        // If there is no active game, something is wrong with seeding
        if (game == null)
        {
            throw new ValidationException("No active game found");
        }

        return game;
    }

    public async Task<List<Game>> GetGamesHistory()
    {
        // Return all games (not soft-deleted), newest first
        return await ctx.Games
            .Where(g => g.Deletedat == null)
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Weeknumber)
            .ToListAsync();
    }

    public async Task<Game> SetWinningNumbers(SetWinningNumbersRequestDto dto)
    {
        // 1) Validate DTO attributes (DataAnnotations)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // 2) Validate winning numbers: exactly 3 distinct numbers between 1 and 16
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

        // 3) Load the game and its boards
        var game = await ctx.Games
            .Include(g => g.Boards)
            .FirstOrDefaultAsync(g => g.Id == dto.GameId && g.Deletedat == null);

        if (game == null)
        {
            throw new ValidationException("Game not found");
        }

        // 4) Do not allow setting winning numbers twice
        if (game.Winningnumbers != null)
        {
            throw new ValidationException("Winning numbers already set for this game");
        }

        // 5) Set winning numbers and close the game
        game.Winningnumbers = sortedNumbers.ToList();
        game.Closedat = timeProvider.GetUtcNow().UtcDateTime;
        game.Isactive = false;

        // 6) Mark winning boards for this game
        // A board is winning if it contains all 3 winning numbers
        var winningSet = sortedNumbers.ToHashSet();

        foreach (var board in game.Boards.Where(b => b.Deletedat == null))
        {
            var boardSet = board.Numbers.ToHashSet();
            var isWinning = winningSet.All(n => boardSet.Contains(n));
            board.Iswinning = isWinning;
        }

        // 7) Activate the next upcoming game (same idea as seeding future games)
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

        // 8) Save all changes as a single transaction
        await ctx.SaveChangesAsync();

        return game;
    }
}
