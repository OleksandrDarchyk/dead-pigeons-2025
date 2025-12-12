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

/// <summary>
/// Service responsible for creating and reading boards (tickets) for games.
/// </summary>
public class BoardService(
    MyDbContext ctx,
    TimeProvider timeProvider) : IBoardService
{
    /// <summary>
    /// Creates a new board for a given player and game.
    ///
    /// Important business rules:
    /// - Only ACTIVE players can buy boards.
    /// - Boards can only be bought for ACTIVE games (not closed, not deleted).
    /// - Boards can only be bought until Saturday 17:00 Danish local time
    ///   for the corresponding game week.
    /// - Price is calculated on the server based on the number of numbers:
    ///     5 -> 20, 6 -> 40, 7 -> 80, 8 -> 160.
    /// - Balance check:
    ///     We only charge for ONE game (this week) here.
    ///     RepeatWeeks does NOT prepay future games.
    ///     Future repeat boards will be created by GameService
    ///     and will charge the weekly price again at that time.
    /// </summary>
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

        // Business rule: players may only join the game until
        // Saturday 17:00 Danish local time (for this game's week/year).
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var dkZone = GetDanishTimeZone();
        var nowDk = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, dkZone);

        // ISO week start (Monday) for this game (year + weekNumber)
        // We treat it as Danish local date.
        var weekStartDk = ISOWeek.ToDateTime(game.Year, game.Weeknumber, DayOfWeek.Monday);

        // Saturday of this ISO week at 17:00 Danish local time
        var saturdayDeadlineDk = weekStartDk
            .AddDays(5)          // Monday + 5 days = Saturday
            .AddHours(17);       // 17:00 (5 PM)

        // "Until 5 o'clock" means 16:59:59 is allowed, 17:00:00 is not.
        if (nowDk >= saturdayDeadlineDk)
        {
            throw new ValidationException(
                "You can no longer join this week's game. " +
                "The deadline is Saturday at 17:00 Danish local time.");
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

        // We only charge for ONE game (this game) here.
        var totalCostForThisGame = weeklyPrice;

        // Balance = sum(approved transactions) - sum(board.Price)
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

            // Price is ALWAYS the weekly price for a single game.
            // There is no "prepaid multi-week" price stored in this column.
            Price        = weeklyPrice,

            Iswinning    = false,

            // How many FUTURE games this board should repeat for.
            // Example: RepeatWeeks = 2 -> this board should auto-repeat
            // for the next 2 games (if repeat is still active and balance is OK).
            Repeatweeks  = dto.RepeatWeeks,

            // Auto-repeat is active only if there are any future repeats requested.
            Repeatactive = dto.RepeatWeeks > 0,

            Createdat    = now,
            Deletedat    = null
        };

        ctx.Boards.Add(board);
        await ctx.SaveChangesAsync();

        return board;
    }

    /// <summary>
    /// Returns all non-deleted boards for a specific game,
    /// including Player and Game navigation properties.
    /// Used mostly for admin overview.
    /// </summary>
    public async Task<List<Board>> GetBoardsForGame(string gameId)
    {
        return await ctx.Boards
            .Where(b => b.Gameid == gameId && b.Deletedat == null)
            .Include(b => b.Player) // used for admin overview (player details)
            .Include(b => b.Game)   // used for GameWeek / GameYear in DTO
            .OrderBy(b => b.Createdat)
            .ToListAsync();
    }

    /// <summary>
    /// Returns all non-deleted boards for a specific player (latest first),
    /// including Game navigation property for week/year metadata.
    /// </summary>
    public async Task<List<Board>> GetBoardsForPlayer(string playerId)
    {
        return await ctx.Boards
            .Where(b => b.Playerid == playerId && b.Deletedat == null)
            .Include(b => b.Game) // used to expose GameWeek / GameYear in DTO
            .OrderByDescending(b => b.Createdat)
            .ToListAsync();
    }

    /// <summary>
    /// Creates a board for the player connected to the current JWT user.
    /// The email is resolved from claims and mapped to a Player entity.
    /// </summary>
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

    /// <summary>
    /// Returns all boards for the current JWT user (resolved via email -> Player).
    /// </summary>
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
    /// Stop repeating for a board that belongs to the current logged-in user.
    /// We do NOT refund anything, because we never prepaid future games.
    /// We just prevent creating new boards in future games.
    /// </summary>
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

        // If already stopped, we can just return it as-is
        if (!board.Repeatactive && board.Repeatweeks == 0)
        {
            return board;
        }

        // Disable auto-repeat for future games
        board.Repeatactive = false;
        board.Repeatweeks = 0;

        await ctx.SaveChangesAsync();

        return board;
    }

    /// <summary>
    /// Calculates current balance for a player as:
    ///   sum(Approved transactions.amount) - sum(boards.Price).
    ///
    /// Important:
    /// - For ALL boards (normal or "repeat"), Price is ALWAYS the weekly price
    ///   for a single game (20 / 40 / 80 / 160).
    /// - When a board is repeated to a future game, that new board also
    ///   has Price = weekly price, and balance is checked again at creation time.
    /// - There are no "zero-price" repeat copies anymore; every board
    ///   that exists in the database represents money actually spent
    ///   for that specific game.
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

    /// <summary>
    /// Returns the Danish time zone (Europe/Copenhagen) if available.
    /// Falls back to local server time zone if it cannot be resolved.
    /// This keeps the "Danish local time" rule robust across environments.
    /// </summary>
    private static TimeZoneInfo GetDanishTimeZone()
    {
        try
        {
            // Linux / macOS (IANA time zone id)
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Windows fallback with a matching offset/DST rules
                return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Final fallback: use server local time zone
                return TimeZoneInfo.Local;
            }
        }
        catch (InvalidTimeZoneException)
        {
            // If something is wrong with the time zone data, use local as best effort
            return TimeZoneInfo.Local;
        }
    }
}
