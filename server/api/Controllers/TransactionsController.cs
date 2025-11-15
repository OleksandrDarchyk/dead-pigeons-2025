using api.Models.Requests;
using api.Models.Responses;
using api.Services;
using dataccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    // POST /CreateTransaction
    // Player submits a new MobilePay transaction (status = Pending)
    [HttpPost(nameof(CreateTransaction))]
    public async Task<Transaction> CreateTransaction([FromBody] CreateTransactionRequestDto dto)
    {
        // Later we can add [Authorize] and check that the playerId matches the logged in user
        var tx = await transactionService.CreateTransaction(dto);
        return tx;
    }

    // POST /ApproveTransaction?transactionId=...
    // Admin approves a pending transaction
    [HttpPost(nameof(ApproveTransaction))]
    public async Task<Transaction> ApproveTransaction([FromQuery] string transactionId)
    {
        // Later we can add [Authorize(Roles = "Admin")]
        var tx = await transactionService.ApproveTransaction(transactionId);
        return tx;
    }

    // POST /RejectTransaction?transactionId=...
    // Admin rejects a pending transaction
    [HttpPost(nameof(RejectTransaction))]
    public async Task<Transaction> RejectTransaction([FromQuery] string transactionId)
    {
        // Later we can add [Authorize(Roles = "Admin")]
        var tx = await transactionService.RejectTransaction(transactionId);
        return tx;
    }

    // GET /GetTransactionsForPlayer?playerId=...
    // Returns transaction history for a specific player
    [HttpGet(nameof(GetTransactionsForPlayer))]
    public async Task<List<Transaction>> GetTransactionsForPlayer([FromQuery] string playerId)
    {
        // Later we can add [Authorize] and ensure players only see their own transactions
        var list = await transactionService.GetTransactionsForPlayer(playerId);
        return list;
    }

    // GET /GetPendingTransactions
    // Admin overview: list of all pending transactions
    [HttpGet(nameof(GetPendingTransactions))]
    public async Task<List<Transaction>> GetPendingTransactions()
    {
        // Later we can add [Authorize(Roles = "Admin")]
        var list = await transactionService.GetPendingTransactions();
        return list;
    }

    // GET /GetPlayerBalance?playerId=...
    // Returns the current balance for a player
    [HttpGet(nameof(GetPlayerBalance))]
    public async Task<PlayerBalanceResponseDto> GetPlayerBalance([FromQuery] string playerId)
    {
        // Later we can add [Authorize] and ensure players only see their own balance
        var balance = await transactionService.GetPlayerBalance(playerId);
        return balance;
    }
}
