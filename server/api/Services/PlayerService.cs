using System.ComponentModel.DataAnnotations;
using api.Models.Requests;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = Bogus.ValidationException;

namespace api.Services;

// Handles CRUD and status changes for players
public class PlayerService(
    MyDbContext ctx,
    TimeProvider timeProvider) : IPlayerService
{
    public async Task<Player> CreatePlayer(CreatePlayerRequestDto dto)
    {
        // Basic attribute validation (required, length, etc.)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // Email must be unique among non-deleted players
        var emailTaken = await ctx.Players.AnyAsync(p =>
            p.Email == dto.Email && p.Deletedat == null);

        if (emailTaken)
        {
            throw new ValidationException("Player with this email already exists.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // New players start as inactive by business rule
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

    public async Task<List<Player>> GetPlayers(bool? isActive = null)
    {
        // Start from non-deleted players only
        var query = ctx.Players
            .Where(p => p.Deletedat == null);

        // Optional filter by active flag
        if (isActive.HasValue)
        {
            query = query.Where(p => p.Isactive == isActive.Value);
        }

        // Stable ordering by name
        return await query
            .OrderBy(p => p.Fullname)
            .ToListAsync();
    }

    public async Task<Player> ActivatePlayer(string playerId)
    {
        // Only non-deleted players can be activated
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
        // Soft delete: mark as deleted instead of removing the row
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
        // Only return non-deleted players
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
        // Attribute-level validation on the DTO
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

        // If email is changed, check that it is still unique
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

        // Update basic properties
        player.Fullname = dto.FullName;
        player.Phone = dto.Phone;

        await ctx.SaveChangesAsync();

        return player;
    }
}
