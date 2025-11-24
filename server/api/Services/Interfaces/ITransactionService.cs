// api/Services/ITransactionService.cs
using System.Security.Claims;
using api.Models.Requests;
using api.Models.Responses;
using dataccess.Entities;

namespace api.Services;

public interface ITransactionService
{
    // Creates a new MobilePay transaction with "Pending" status
    Task<Transaction> CreateTransaction(CreateTransactionRequestDto dto);

    // Creates a new transaction for the currently logged-in user (resource-based)
    Task<Transaction> CreateTransactionForCurrentUser(ClaimsPrincipal claims, CreateTransactionRequestDto dto);

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