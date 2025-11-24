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
// All endpoints here require authentication by default
[Authorize]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    // --------------------------------
    // PLAYER: create own transaction
    // --------------------------------
    [HttpPost(nameof(CreateTransaction))]
    public async Task<Transaction> CreateTransaction([FromBody] CreateTransactionRequestDto dto)
    {
        // PlayerId is resolved from JWT, not trusted from the body
        return await transactionService.CreateTransactionForCurrentUser(User, dto);
    }

    // --------------------------------
    // ADMIN: approve transaction
    // --------------------------------
    [HttpPost(nameof(ApproveTransaction))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<Transaction> ApproveTransaction([FromQuery] string transactionId)
    {
        return await transactionService.ApproveTransaction(transactionId);
    }

    // --------------------------------
    // ADMIN: reject transaction
    // --------------------------------
    [HttpPost(nameof(RejectTransaction))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<Transaction> RejectTransaction([FromQuery] string transactionId)
    {
        return await transactionService.RejectTransaction(transactionId);
    }

    // --------------------------------
    // PLAYER: get own transactions
    // --------------------------------
    [HttpGet(nameof(GetMyTransactions))]
    public async Task<List<Transaction>> GetMyTransactions()
    {
        return await transactionService.GetTransactionsForCurrentUser(User);
    }

    // --------------------------------
    // ADMIN: all pending transactions
    // --------------------------------
    [HttpGet(nameof(GetPendingTransactions))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<Transaction>> GetPendingTransactions()
    {
        return await transactionService.GetPendingTransactions();
    }

    // --------------------------------
    // PLAYER: get own balance
    // --------------------------------
    [HttpGet(nameof(GetMyBalance))]
    public async Task<PlayerBalanceResponseDto> GetMyBalance()
    {
        return await transactionService.GetBalanceForCurrentUser(User);
    }

    // --------------------------------
    // ADMIN: get balance for a player
    // --------------------------------
    [HttpGet(nameof(GetPlayerBalance))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<PlayerBalanceResponseDto> GetPlayerBalance([FromQuery] string playerId)
    {
        // Simple admin helper: reuse shared balance logic
        return await transactionService.GetPlayerBalance(playerId);
    }
}
