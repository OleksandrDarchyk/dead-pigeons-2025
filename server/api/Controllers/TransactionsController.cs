// api/Controllers/TransactionsController.cs
using api.Models.Requests;
using api.Models.Responses;
using api.Services;
using dataccess.Entities;
using Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize] // all actions require authenticated user unless overridden
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    // Player creates a transaction for their own account (player id comes from JWT)
    [HttpPost(nameof(CreateTransaction))]
    public async Task<Transaction> CreateTransaction([FromBody] CreateTransactionRequestDto dto)
    {
        return await transactionService.CreateTransactionForCurrentUser(User, dto);
    }

    // Admin creates a transaction for a chosen player
    [HttpPost(nameof(CreateTransactionForPlayer))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<Transaction> CreateTransactionForPlayer([FromBody] CreateTransactionRequestDto dto)
    {
        return await transactionService.CreateTransaction(dto);
    }

    // Admin approves a pending transaction
    [HttpPost(nameof(ApproveTransaction))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<Transaction> ApproveTransaction([FromQuery] string transactionId)
    {
        return await transactionService.ApproveTransaction(transactionId);
    }

    // Admin rejects a pending transaction
    [HttpPost(nameof(RejectTransaction))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<Transaction> RejectTransaction([FromQuery] string transactionId)
    {
        return await transactionService.RejectTransaction(transactionId);
    }

    // Player gets their own transactions
    [HttpGet(nameof(GetMyTransactions))]
    public async Task<List<Transaction>> GetMyTransactions()
    {
        return await transactionService.GetTransactionsForCurrentUser(User);
    }

    // Admin gets all pending transactions
    [HttpGet(nameof(GetPendingTransactions))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<Transaction>> GetPendingTransactions()
    {
        return await transactionService.GetPendingTransactions();
    }

    // Player gets their own balance
    [HttpGet(nameof(GetMyBalance))]
    public async Task<PlayerBalanceResponseDto> GetMyBalance()
    {
        return await transactionService.GetBalanceForCurrentUser(User);
    }

    // Admin gets balance for a specific player
    [HttpGet(nameof(GetPlayerBalance))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<PlayerBalanceResponseDto> GetPlayerBalance([FromQuery] string playerId)
    {
        return await transactionService.GetPlayerBalance(playerId);
    }
}
