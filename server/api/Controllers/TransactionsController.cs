// api/Controllers/TransactionsController.cs

using api.Models.Responses;
using api.Models.Transactions;
using api.Services;

using Api.Security;
using dataccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize] // all actions require authenticated user unless overridden
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    // Helper: map entity -> response DTO for API
    private static TransactionResponseDto MapToDto(Transaction t) => new()
    {
        Id              = t.Id,
        PlayerId        = t.Playerid,
        MobilePayNumber = t.Mobilepaynumber,
        Amount          = t.Amount,
        Status          = t.Status,
        CreatedAt       = t.Createdat,
        ApprovedAt      = t.Approvedat,
        RejectionReason = t.Rejectionreason
    };

    // Player creates a transaction for their own account (player id comes from JWT)
    [HttpPost(nameof(CreateTransaction))]
    public async Task<TransactionResponseDto> CreateTransaction(
        [FromBody] CreateTransactionForCurrentUserRequestDto dto)
    {
        var transaction = await transactionService.CreateTransactionForCurrentUser(User, dto);
        return MapToDto(transaction);
    }

    // Admin creates a transaction for a chosen player
    [HttpPost(nameof(CreateTransactionForPlayer))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<TransactionResponseDto> CreateTransactionForPlayer(
        [FromBody] AdminCreateTransactionRequestDto dto)
    {
        var transaction = await transactionService.CreateTransaction(dto);
        return MapToDto(transaction);
    }

    // Admin approves a pending transaction
    [HttpPost(nameof(ApproveTransaction))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<TransactionResponseDto> ApproveTransaction([FromQuery] string transactionId)
    {
        var transaction = await transactionService.ApproveTransaction(transactionId);
        return MapToDto(transaction);
    }

    // Admin rejects a pending transaction
    [HttpPost(nameof(RejectTransaction))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<TransactionResponseDto> RejectTransaction([FromQuery] string transactionId)
    {
        var transaction = await transactionService.RejectTransaction(transactionId);
        return MapToDto(transaction);
    }

    // Player gets their own transactions
    [HttpGet(nameof(GetMyTransactions))]
    public async Task<List<TransactionResponseDto>> GetMyTransactions()
    {
        var transactions = await transactionService.GetTransactionsForCurrentUser(User);
        return transactions
            .Select(MapToDto)
            .ToList();
    }

    // Admin gets all pending transactions
    [HttpGet(nameof(GetPendingTransactions))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<TransactionResponseDto>> GetPendingTransactions()
    {
        var transactions = await transactionService.GetPendingTransactions();
        return transactions
            .Select(MapToDto)
            .ToList();
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
