//delete before exam :
//ctx - access to DB

using System.ComponentModel.DataAnnotations;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class PlayerService(
    MyDbContext ctx,
    TimeProvider timeProvider) : IPlayerService
{
    public async Task<Player> CreatePlayer(CreatePlayerRequestDto dto)
    {
        // 1) Validate DTO attributes
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // 2) Check unique email (and not soft-deleted)
        var emailTaken = ctx.Players.Any(p =>
            p.Email == dto.Email && p.Deletedat == null);

        if (emailTaken)
        {
            throw new ValidationException("Player with this email already exists");
        }

        // 3) Create new Player entity
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Fullname = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Isactive = false,          // by default player is inactive (exam rule)
            Activatedat = null,
            Createdat = now,
            Deletedat = null
        };

        // 4) Save to DB
        ctx.Players.Add(player);
        await ctx.SaveChangesAsync();

        return player;
    }

    public async Task<List<Player>> GetPlayers(bool? isActive = null)
    {
        // base query: only not soft-deleted
        var query = ctx.Players
            .Where(p => p.Deletedat == null);

        if (isActive.HasValue)
        {
            query = query.Where(p => p.Isactive == isActive.Value);
        }

        // we can order by CreatedAt to have consistent ordering
        return await query
            .OrderBy(p => p.Fullname)
            .ToListAsync();
    }

    public async Task<Player> ActivatePlayer(string playerId)
    {
        // find not soft-deleted player by ID
        var player = ctx.Players
            .FirstOrDefault(p => p.Id == playerId && p.Deletedat == null);

        // if not found, throw exception
        if (player == null)
        {
            throw new ValidationException("Player not found");
        }

        // if player is inactive, activate it
        if (!player.Isactive)
        {
            player.Isactive = true;
            player.Activatedat = timeProvider.GetUtcNow().UtcDateTime;
        }

        await ctx.SaveChangesAsync();
        return player;
    }

    public async Task<Player> DeactivatePlayer(string playerId)
    {
        var player = ctx.Players
            .FirstOrDefault(p => p.Id == playerId && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found");
        }

        if (player.Isactive)
        {
            player.Isactive = false;
        }

        await ctx.SaveChangesAsync();
        return player;
    }

    public async Task<Player> SoftDeletePlayer(string playerId)
    {
        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Id == playerId && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found or already deleted");
        }

        player.Deletedat = timeProvider.GetUtcNow().UtcDateTime;

        await ctx.SaveChangesAsync();

        return player;
    }

    public async Task<Player> GetPlayerById(string playerId)
    {
        // Only get non-deleted players
        var player = await ctx.Players
            .Where(p => p.Deletedat == null)
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
        {
            // Player does not exist or was soft-deleted
            throw new ValidationException("Player not found");
        }

        return player;
    }

    public async Task<Player> UpdatePlayer(UpdatePlayerRequestDto dto)
    {
        // 1) Validate DTO attributes (DataAnnotations)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // 2) Find existing, not soft-deleted player
        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found");
        }

        // 3) If email is provided and different, check uniqueness
        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != player.Email)
        {
            var emailTaken = await ctx.Players
                .AnyAsync(p =>
                    p.Email == dto.Email &&
                    p.Id != dto.Id &&
                    p.Deletedat == null);

            if (emailTaken)
            {
                throw new ValidationException("Player with this email already exists");
            }

            player.Email = dto.Email;
        }

        // 4) Update other fields (always required in DTO)
        player.Fullname = dto.FullName;
        player.Phone = dto.Phone;

        // 5) Save changes
        await ctx.SaveChangesAsync();

        return player;
    }
}
