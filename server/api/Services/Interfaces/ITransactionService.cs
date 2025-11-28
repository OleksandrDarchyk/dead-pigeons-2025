// api/Services/ITransactionService.cs
using System.Security.Claims;
using api.Models.Responses;
using api.Models.Transactions;
using dataccess.Entities;

namespace api.Services;

public interface ITransactionService
{
    // Admin: create transaction for a specific player
    Task<Transaction> CreateTransaction(AdminCreateTransactionRequestDto dto);

    // Player: create transaction for the currently logged-in user
    Task<Transaction> CreateTransactionForCurrentUser(
        ClaimsPrincipal claims,
        CreateTransactionForCurrentUserRequestDto dto);

    // Approves a pending transaction (admin action)
    Task<Transaction> ApproveTransaction(string transactionId);

    // Rejects a pending transaction (admin action)
    Task<Transaction> RejectTransaction(string transactionId);

    // Returns all non-deleted transactions for a specific player (admin use)
    Task<List<Transaction>> GetTransactionsForPlayer(string playerId);

    // Returns all pending transactions (for admin overview)
    Task<List<Transaction>> GetPendingTransactions();

    // Calculates the current balance for a player:
    // sum(Approved transactions) - sum(boards.Price)
    Task<PlayerBalanceResponseDto> GetPlayerBalance(string playerId);

    // Resource-based: transactions of the current user
    Task<List<Transaction>> GetTransactionsForCurrentUser(ClaimsPrincipal claims);

    // Resource-based: balance of the current user
    Task<PlayerBalanceResponseDto> GetBalanceForCurrentUser(ClaimsPrincipal claims);
}