// api/Services/TransactionService.cs
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using api.Models.Requests;
using api.Models.Responses;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;
using ValidationException = Bogus.ValidationException;

namespace api.Services;

public class TransactionService(
    MyDbContext ctx,
    TimeProvider timeProvider) : ITransactionService
{
    // Create transaction (used by admin path)
    public async Task<Transaction> CreateTransaction(CreateTransactionRequestDto dto)
    {
        // Validate basic DTO attributes
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // Player must exist and not be soft-deleted
        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Id == dto.PlayerId && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

        // Prevent duplicate MobilePay numbers
        var exists = await ctx.Transactions
            .AnyAsync(t => t.Mobilepaynumber == dto.MobilePayNumber && t.Deletedat == null);

        if (exists)
        {
            throw new ValidationException("A transaction with this MobilePay number already exists.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Playerid = dto.PlayerId,
            Mobilepaynumber = dto.MobilePayNumber,
            Amount = dto.Amount,
            Status = "Pending",
            Createdat = now,
            Approvedat = null,
            Deletedat = null
        };

        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();

        return transaction;
    }

    // Create transaction for the currently logged-in user
    public async Task<Transaction> CreateTransactionForCurrentUser(
        ClaimsPrincipal claims,
        CreateTransactionRequestDto dto)
    {
        // Email must be present in token
        var email = claims.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email not found in token.");
        }

        // Look up player by email
        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Email == email && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found for this user.");
        }

        // Force ownership to current player, ignore whatever client sent
        dto.PlayerId = player.Id;

        return await CreateTransaction(dto);
    }

    // Admin: approve transaction
    public async Task<Transaction> ApproveTransaction(string transactionId)
    {
        var transaction = await ctx.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Deletedat == null)
            ?? throw new ValidationException("Transaction not found.");

        if (transaction.Status != "Pending")
        {
            throw new ValidationException("Only pending transactions can be approved.");
        }

        transaction.Status = "Approved";
        transaction.Approvedat = timeProvider.GetUtcNow().UtcDateTime;

        await ctx.SaveChangesAsync();
        return transaction;
    }

    // Admin: reject transaction
    public async Task<Transaction> RejectTransaction(string transactionId)
    {
        var transaction = await ctx.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Deletedat == null)
            ?? throw new ValidationException("Transaction not found.");

        if (transaction.Status != "Pending")
        {
            throw new ValidationException("Only pending transactions can be rejected.");
        }

        transaction.Status = "Rejected";
        transaction.Approvedat = null;

        await ctx.SaveChangesAsync();
        return transaction;
    }

    // Admin: all transactions for a specific player
    public async Task<List<Transaction>> GetTransactionsForPlayer(string playerId)
    {
        return await ctx.Transactions
            .Where(t => t.Playerid == playerId && t.Deletedat == null)
            .OrderByDescending(t => t.Createdat)
            .ToListAsync();
    }

    // Admin: all pending transactions
    public async Task<List<Transaction>> GetPendingTransactions()
    {
        return await ctx.Transactions
            .Where(t => t.Deletedat == null && t.Status == "Pending")
            .OrderBy(t => t.Createdat)
            .ToListAsync();
    }

    // Current user: balance based on own boards and approved payments
    public async Task<PlayerBalanceResponseDto> GetBalanceForCurrentUser(ClaimsPrincipal claims)
    {
        var email = claims.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new ValidationException("Email not found in token.");

        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Email == email && p.Deletedat == null)
            ?? throw new ValidationException("Player not found for this user.");

        return await GetPlayerBalance(player.Id);
    }

    // Current user: own transactions
    public async Task<List<Transaction>> GetTransactionsForCurrentUser(ClaimsPrincipal claims)
    {
        var email = claims.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new ValidationException("Email not found in token.");

        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Email == email && p.Deletedat == null)
            ?? throw new ValidationException("Player not found for this user.");

        return await GetTransactionsForPlayer(player.Id);
    }

    // Shared balance calculation logic
    public async Task<PlayerBalanceResponseDto> GetPlayerBalance(string playerId)
    {
        var playerExists = await ctx.Players
            .AnyAsync(p => p.Id == playerId && p.Deletedat == null);

        if (!playerExists)
        {
            throw new ValidationException("Player not found.");
        }

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

        return new PlayerBalanceResponseDto
        {
            PlayerId = playerId,
            Balance = approvedAmount - spentOnBoards
        };
    }
}
