using api.Models.Requests;
using api.Models.Responses;
using dataccess.Entities;

namespace api.Services;

public interface ITransactionService
{
    // Creates a new MobilePay transaction with "Pending" status
    Task<Transaction> CreateTransaction(CreateTransactionRequestDto dto);

    // Approves a pending transaction (admin action)
    Task<Transaction> ApproveTransaction(string transactionId);

    // Rejects a pending transaction (admin action)
    Task<Transaction> RejectTransaction(string transactionId);

    // Returns all non-deleted transactions for a specific player
    Task<List<Transaction>> GetTransactionsForPlayer(string playerId);

    // Returns all pending transactions (for admin overview)
    Task<List<Transaction>> GetPendingTransactions();

    // Calculates the current balance for a player:
    // sum(Approved transactions) - sum(boards.price)
    Task<PlayerBalanceResponseDto> GetPlayerBalance(string playerId);
}