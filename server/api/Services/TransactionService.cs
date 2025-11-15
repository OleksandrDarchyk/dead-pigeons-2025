using System.ComponentModel.DataAnnotations;
using api.Models.Requests;
using api.Models.Responses;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class TransactionService(
    MyDbContext ctx,
    TimeProvider timeProvider) : ITransactionService
{
    public async Task<Transaction> CreateTransaction(CreateTransactionRequestDto dto)
    {
        // 1) Validate DTO attributes (DataAnnotations)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // 2) Ensure player exists and is not soft-deleted
        var player = await ctx.Players
            .FirstOrDefaultAsync(p => p.Id == dto.PlayerId && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

        // 3) Ensure the MobilePay number is unique (to avoid duplicate submissions)
        var exists = await ctx.Transactions
            .AnyAsync(t => t.Mobilepaynumber == dto.MobilePayNumber && t.Deletedat == null);

        if (exists)
        {
            throw new ValidationException("A transaction with this MobilePay number already exists.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // 4) Create the transaction with status "Pending"
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

    public async Task<Transaction> ApproveTransaction(string transactionId)
    {
        // 1) Load the transaction
        var transaction = await ctx.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Deletedat == null);

        if (transaction == null)
        {
            throw new ValidationException("Transaction not found.");
        }

        // 2) Only "Pending" transactions can be approved
        if (transaction.Status == "Approved")
        {
            throw new ValidationException("Transaction is already approved.");
        }

        if (transaction.Status == "Rejected")
        {
            throw new ValidationException("Rejected transactions cannot be approved.");
        }

        // 3) Approve the transaction
        transaction.Status = "Approved";
        transaction.Approvedat = timeProvider.GetUtcNow().UtcDateTime;

        await ctx.SaveChangesAsync();

        return transaction;
    }

    public async Task<Transaction> RejectTransaction(string transactionId)
    {
        // 1) Load the transaction
        var transaction = await ctx.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Deletedat == null);

        if (transaction == null)
        {
            throw new ValidationException("Transaction not found.");
        }

        // 2) Only "Pending" transactions can be rejected
        if (transaction.Status == "Approved")
        {
            throw new ValidationException("Approved transactions cannot be rejected.");
        }

        if (transaction.Status == "Rejected")
        {
            throw new ValidationException("Transaction is already rejected.");
        }

        // 3) Reject the transaction
        transaction.Status = "Rejected";
        transaction.Approvedat = null;

        await ctx.SaveChangesAsync();

        return transaction;
    }

    public async Task<List<Transaction>> GetTransactionsForPlayer(string playerId)
    {
        // Return all non-deleted transactions for this player (latest first)
        return await ctx.Transactions
            .Where(t => t.Playerid == playerId && t.Deletedat == null)
            .OrderByDescending(t => t.Createdat)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetPendingTransactions()
    {
        // Return all non-deleted "Pending" transactions
        return await ctx.Transactions
            .Where(t => t.Deletedat == null && t.Status == "Pending")
            .OrderBy(t => t.Createdat)
            .ToListAsync();
    }

    public async Task<PlayerBalanceResponseDto> GetPlayerBalance(string playerId)
    {
        // 1) Ensure player exists (not soft-deleted)
        var playerExists = await ctx.Players
            .AnyAsync(p => p.Id == playerId && p.Deletedat == null);

        if (!playerExists)
        {
            throw new ValidationException("Player not found.");
        }

        // 2) Sum of all approved transactions for this player
        var approvedAmount = await ctx.Transactions
            .Where(t =>
                t.Playerid == playerId &&
                t.Deletedat == null &&
                t.Status == "Approved")
            .SumAsync(t => (int?)t.Amount) ?? 0;

        // 3) Sum of all board prices bought by this player
        var spentOnBoards = await ctx.Boards
            .Where(b =>
                b.Playerid == playerId &&
                b.Deletedat == null)
            .SumAsync(b => (int?)b.Price) ?? 0;

        var balance = approvedAmount - spentOnBoards;

        return new PlayerBalanceResponseDto
        {
            PlayerId = playerId,
            Balance = balance
        };
    }
}
