// api/Services/TransactionService.cs
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using api.Models.Responses;
using api.Models.Transactions;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class TransactionService : ITransactionService
{
    private readonly MyDbContext _ctx;
    private readonly TimeProvider _timeProvider;

    // Local status constants to avoid magic strings
    private const string StatusPending  = "Pending";
    private const string StatusApproved = "Approved";
    private const string StatusRejected = "Rejected";

    public TransactionService(MyDbContext ctx, TimeProvider timeProvider)
    {
        _ctx = ctx;
        _timeProvider = timeProvider;
    }

    // Admin: create transaction for a specific player
    public async Task<Transaction> CreateTransaction(AdminCreateTransactionRequestDto dto)
    {
        // Validate DTO attributes (Required, MinLength, Range)
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // Player must exist and not be soft-deleted
        var player = await _ctx.Players
            .FirstOrDefaultAsync(p => p.Id == dto.PlayerId && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found.");
        }

        // Prevent duplicate MobilePay numbers (among non-deleted transactions)
        var exists = await _ctx.Transactions
            .AnyAsync(t => t.Mobilepaynumber == dto.MobilePayNumber && t.Deletedat == null);

        if (exists)
        {
            throw new ValidationException("A transaction with this MobilePay number already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var transaction = new Transaction
        {
            Id              = Guid.NewGuid().ToString(),
            Playerid        = dto.PlayerId,
            Mobilepaynumber = dto.MobilePayNumber,
            Amount          = dto.Amount,
            Status          = StatusPending,
            Createdat       = now,
            Approvedat      = null,
            Deletedat       = null,
            Rejectionreason = null
        };

        _ctx.Transactions.Add(transaction);
        await _ctx.SaveChangesAsync();

        return transaction;
    }

    // Player: create transaction for the currently logged-in user
    public async Task<Transaction> CreateTransactionForCurrentUser(
        ClaimsPrincipal claims,
        CreateTransactionForCurrentUserRequestDto dto)
    {
        // Validate basic DTO attributes
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);

        // Email must be present in token
        var email = claims.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email not found in token.");
        }

        // Look up player by email
        var player = await _ctx.Players
            .FirstOrDefaultAsync(p => p.Email == email && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found for this user.");
        }

        // Reuse admin creation logic with correct PlayerId
        var adminDto = new AdminCreateTransactionRequestDto
        {
            PlayerId        = player.Id,
            MobilePayNumber = dto.MobilePayNumber,
            Amount          = dto.Amount
        };

        return await CreateTransaction(adminDto);
    }

    // Admin: approve transaction
    public async Task<Transaction> ApproveTransaction(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ValidationException("Transaction id is required.");
        }

        var transaction = await _ctx.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Deletedat == null)
            ?? throw new ValidationException("Transaction not found.");

        if (transaction.Status != StatusPending)
        {
            throw new ValidationException("Only pending transactions can be approved.");
        }

        transaction.Status = StatusApproved;
        transaction.Approvedat = _timeProvider.GetUtcNow().UtcDateTime;
        transaction.Rejectionreason = null;

        await _ctx.SaveChangesAsync();
        return transaction;
    }

    // Admin: reject transaction
    public async Task<Transaction> RejectTransaction(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ValidationException("Transaction id is required.");
        }

        var transaction = await _ctx.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Deletedat == null)
            ?? throw new ValidationException("Transaction not found.");

        if (transaction.Status != StatusPending)
        {
            throw new ValidationException("Only pending transactions can be rejected.");
        }

        transaction.Status = StatusRejected;
        transaction.Approvedat = null;
        // Optionally later you can add a Reject(reason) overload and set Rejectionreason

        await _ctx.SaveChangesAsync();
        return transaction;
    }

    // Admin: all transactions for a specific player
    public async Task<List<Transaction>> GetTransactionsForPlayer(string playerId)
    {
        // For read-only list AsNoTracking is fine
        return await _ctx.Transactions
            .Where(t => t.Playerid == playerId && t.Deletedat == null)
            .Include(t => t.Player)
            .OrderByDescending(t => t.Createdat)
            .AsNoTracking()
            .ToListAsync();
    }

    // Admin: all pending transactions
    public async Task<List<Transaction>> GetPendingTransactions()
    {
        return await _ctx.Transactions
            .Where(t => t.Deletedat == null && t.Status == StatusPending)
            .Include(t => t.Player)
            .OrderBy(t => t.Createdat)
            .AsNoTracking()
            .ToListAsync();
    }

    //  history (Approved/Rejected, optionally filtered)
    // ✅ History (Approved/Rejected, optionally filtered)
    public async Task<List<Transaction>> GetTransactionsHistory(
        string? playerId = null,
        string? status = null)
    {
        IQueryable<Transaction> query = _ctx.Transactions
            .Where(t => t.Deletedat == null);
        
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            query = query.Where(t => t.Playerid == playerId);
        }
        
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status != StatusPending &&
                status != StatusApproved &&
                status != StatusRejected)
            {
                throw new ValidationException("Invalid transaction status filter.");
            }

            query = query.Where(t => t.Status == status);
        }
        else
        {
            query = query.Where(t => t.Status != StatusPending);
        }
        
        return await query
            .Include(t => t.Player)
            .OrderByDescending(t => t.Createdat)
            .AsNoTracking()
            .ToListAsync();
    }


    // Current user: balance based on own boards and approved payments
    public async Task<PlayerBalanceResponseDto> GetBalanceForCurrentUser(ClaimsPrincipal claims)
    {
        var email = claims.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email not found in token.");
        }

        var player = await _ctx.Players
            .FirstOrDefaultAsync(p => p.Email == email && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found for this user.");
        }

        return await GetPlayerBalance(player.Id);
    }

    // Current user: own transactions
    public async Task<List<Transaction>> GetTransactionsForCurrentUser(ClaimsPrincipal claims)
    {
        var email = claims.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email not found in token.");
        }

        var player = await _ctx.Players
            .FirstOrDefaultAsync(p => p.Email == email && p.Deletedat == null);

        if (player == null)
        {
            throw new ValidationException("Player not found for this user.");
        }

        return await GetTransactionsForPlayer(player.Id);
    }

    // Shared balance calculation logic
    public async Task<PlayerBalanceResponseDto> GetPlayerBalance(string playerId)
    {
        var playerExists = await _ctx.Players
            .AnyAsync(p => p.Id == playerId && p.Deletedat == null);

        if (!playerExists)
        {
            throw new ValidationException("Player not found.");
        }

        // Sum of approved transactions
        var approvedAmount = await _ctx.Transactions
            .Where(t =>
                t.Playerid == playerId &&
                t.Deletedat == null &&
                t.Status == StatusApproved)
            .SumAsync(t => (int?)t.Amount) ?? 0;

        // Sum of all board prices for this player
        var spentOnBoards = await _ctx.Boards
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
