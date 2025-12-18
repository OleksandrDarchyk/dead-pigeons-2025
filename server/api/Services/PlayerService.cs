using System.ComponentModel.DataAnnotations;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = Bogus.ValidationException;

namespace api.Services;

public class PlayerService(
    MyDbContext ctx,
    TimeProvider timeProvider) : IPlayerService
{
    public async Task<Player> CreatePlayer(CreatePlayerRequestDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var emailTaken = await ctx.Players.AnyAsync(p =>
            p.Email == dto.Email && p.Deletedat == null);

        if (emailTaken)
        {
            throw new ValidationException("Player with this email already exists.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Fullname = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Isactive = false,
            Activatedat = null,
            Createdat = now,
            Deletedat = null
        };

        ctx.Players.Add(player);
        await ctx.SaveChangesAsync();

        return player;
    }

    public async Task<List<Player>> GetPlayers(
        bool? isActive = null,
        string? sortBy = null,
        string? direction = null)
    {
        var query = ctx.Players
            .Where(p => p.Deletedat == null);
        
        if (isActive.HasValue)
        {
            query = query.Where(p => p.Isactive == isActive.Value);
        }
        
        var desc = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        
        var sort = (sortBy ?? "").ToLowerInvariant();
        
        query = sort switch
        {
            "fullname" => desc
                ? query.OrderByDescending(p => p.Fullname)
                : query.OrderBy(p => p.Fullname),

            "email" => desc
                ? query.OrderByDescending(p => p.Email)
                : query.OrderBy(p => p.Email),

            "isactive" => desc
                ? query.OrderByDescending(p => p.Isactive)
                : query.OrderBy(p => p.Isactive),

            "activatedat" => desc
                ? query.OrderByDescending(p => p.Activatedat ?? DateTime.MinValue)
                : query.OrderBy(p => p.Activatedat ?? DateTime.MaxValue),

            "createdat" => desc
                ? query.OrderByDescending(p => p.Createdat ?? DateTime.MinValue)
                : query.OrderBy(p => p.Createdat ?? DateTime.MaxValue),
            
            _ => query.OrderBy(p => p.Fullname)
        };

        return await query.ToListAsync();
    }



    public async Task<Player> ActivatePlayer(string playerId)
    {
        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Id == playerId && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

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
        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Id == playerId && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
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
            throw new ValidationException("Player not found or already deleted.");
        }

        player.Deletedat = timeProvider.GetUtcNow().UtcDateTime;

        await ctx.SaveChangesAsync();
        return player;
    }

    public async Task<Player> GetPlayerById(string playerId)
    {
        var player = await ctx.Players
            .Where(p => p.Deletedat == null)
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

        return player;
    }

    public async Task<Player> UpdatePlayer(UpdatePlayerRequestDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != player.Email)
        {
            var emailTaken = await ctx.Players
                .AnyAsync(p =>
                    p.Email == dto.Email &&
                    p.Id != dto.Id &&
                    p.Deletedat == null);

            if (emailTaken)
            {
                throw new ValidationException("Player with this email already exists.");
            }

            player.Email = dto.Email;
        }

        player.Fullname = dto.FullName;
        player.Phone = dto.Phone;

        await ctx.SaveChangesAsync();

        return player;
    }
}
