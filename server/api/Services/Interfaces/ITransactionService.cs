// api/Services/ITransactionService.cs
using System.Security.Claims;
using api.Models.Responses;
using api.Models.Transactions;
using dataccess.Entities;

namespace api.Services;

public interface ITransactionService
{ 
    Task<Transaction> CreateTransaction(AdminCreateTransactionRequestDto dto);
    Task<Transaction> CreateTransactionForCurrentUser(
        ClaimsPrincipal claims,
        CreateTransactionForCurrentUserRequestDto dto);
    Task<Transaction> ApproveTransaction(string transactionId);
    Task<Transaction> RejectTransaction(string transactionId);
    Task<List<Transaction>> GetTransactionsForPlayer(string playerId);
    Task<List<Transaction>> GetPendingTransactions();
    Task<List<Transaction>> GetTransactionsHistory(
        string? playerId = null,
        string? status = null);
    
    Task<PlayerBalanceResponseDto> GetPlayerBalance(string playerId);
    Task<List<Transaction>> GetTransactionsForCurrentUser(ClaimsPrincipal claims);
    Task<PlayerBalanceResponseDto> GetBalanceForCurrentUser(ClaimsPrincipal claims);
}