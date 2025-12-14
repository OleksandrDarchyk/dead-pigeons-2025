// api/Etc/GameSeeder.cs
using System.Data;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Etc;

/// <summary>
/// Tip 1 (State-less API) seeder:
/// - No scheduler/cron: games are pre-seeded for ~20 years.
/// - Idempotent: safe to run on every startup.
/// - Guarantees exactly one active game and enough future inactive games.
/// </summary>
public class GameSeeder(MyDbContext ctx, TimeProvider timeProvider)
{
    private const int WeeksToCreate = 20 * 52; // ~20 years forward using a 1..52 week model

    public async Task SeedGamesIfMissingAsync(CancellationToken ct = default)
    {
        await using var tx = await ctx.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var existingKeys = await ctx.Games
            .Select(g => new { g.Year, g.Weeknumber })
            .ToListAsync(ct);

        var existingSet = new HashSet<(int year, int week)>(existingKeys.Select(x => (x.Year, x.Weeknumber)));

        var activeGame = await EnsureExactlyOneActiveGameAsync(existingSet, ct);
        await EnsureFutureGamesFromActiveAsync(activeGame, existingSet, ct);

        await tx.CommitAsync(ct);
    }

    private async Task<Game> EnsureExactlyOneActiveGameAsync(
        HashSet<(int year, int week)> existingSet,
        CancellationToken ct)
    {
        var activeGames = await ctx.Games
            .Where(g => g.Deletedat == null && g.Isactive)
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Weeknumber)
            .ToListAsync(ct);

        if (activeGames.Count > 1)
        {
            throw new InvalidOperationException("Invariant violated: more than one active game exists.");
        }

        if (activeGames.Count == 1)
        {
            return activeGames[0];
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var year = now.Year;
        var week = GetWeekNumberSimple(now);

        // Prefer activating the current (year, week). If it exists but is soft-deleted, undelete it.
        var current = await ctx.Games
            .Where(g => g.Year == year && g.Weeknumber == week)
            .FirstOrDefaultAsync(ct);

        if (current == null)
        {
            var newActive = new Game
            {
                Id = Guid.NewGuid().ToString(),
                Year = year,
                Weeknumber = week,
                Isactive = true,
                Createdat = now,
                Closedat = null,
                Deletedat = null,
                Winningnumbers = null
            };

            ctx.Games.Add(newActive);
            existingSet.Add((year, week));
            await ctx.SaveChangesAsync(ct);
            return newActive;
        }

        if (current.Deletedat != null)
        {
            current.Deletedat = null;
        }

        // Defensive: if DB state is broken, force all other games to inactive
        await ctx.Games
            .Where(g => g.Deletedat == null && g.Isactive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Isactive, false), ct);

        current.Isactive = true;
        await ctx.SaveChangesAsync(ct);
        return current;
    }

    private async Task EnsureFutureGamesFromActiveAsync(
        Game active,
        HashSet<(int year, int week)> existingSet,
        CancellationToken ct)
    {
        var yearCursor = active.Year;
        var weekCursor = active.Weeknumber;

        var createdCursor = active.Createdat ?? timeProvider.GetUtcNow().UtcDateTime;

        for (var i = 0; i < WeeksToCreate; i++)
        {
            MoveNextWeek(ref yearCursor, ref weekCursor);
            createdCursor = createdCursor.AddDays(7);

            if (existingSet.Contains((yearCursor, weekCursor)))
            {
                continue;
            }

            ctx.Games.Add(new Game
            {
                Id = Guid.NewGuid().ToString(),
                Year = yearCursor,
                Weeknumber = weekCursor,
                Isactive = false,
                Createdat = createdCursor,
                Closedat = null,
                Deletedat = null,
                Winningnumbers = null
            });

            existingSet.Add((yearCursor, weekCursor));
        }

        await ctx.SaveChangesAsync(ct);
    }

    private static int GetWeekNumberSimple(DateTime date)
    {
        var firstDay = new DateTime(date.Year, 1, 1);
        var days = (date - firstDay).Days;
        var week = (days / 7) + 1;
        return week > 52 ? 52 : week;
    }

    private static void MoveNextWeek(ref int year, ref int week)
    {
        if (week < 52)
        {
            week++;
        }
        else
        {
            week = 1;
            year++;
        }
    }
}
