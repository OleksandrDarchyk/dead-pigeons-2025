
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
[Authorize] 
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
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

    [HttpPost(nameof(CreateTransaction))]
    public async Task<TransactionResponseDto> CreateTransaction(
        [FromBody] CreateTransactionForCurrentUserRequestDto dto)
    {
        var transaction = await transactionService.CreateTransactionForCurrentUser(User, dto);
        return MapToDto(transaction);
    }

    [HttpPost(nameof(CreateTransactionForPlayer))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<TransactionResponseDto> CreateTransactionForPlayer(
        [FromBody] AdminCreateTransactionRequestDto dto)
    {
        var transaction = await transactionService.CreateTransaction(dto);
        return MapToDto(transaction);
    }

    [HttpPost(nameof(ApproveTransaction))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<TransactionResponseDto> ApproveTransaction([FromQuery] string transactionId)
    {
        var transaction = await transactionService.ApproveTransaction(transactionId);
        return MapToDto(transaction);
    }

    [HttpPost(nameof(RejectTransaction))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<TransactionResponseDto> RejectTransaction([FromQuery] string transactionId)
    {
        var transaction = await transactionService.RejectTransaction(transactionId);
        return MapToDto(transaction);
    }

    [HttpGet(nameof(GetMyTransactions))]
    public async Task<List<TransactionResponseDto>> GetMyTransactions()
    {
        var transactions = await transactionService.GetTransactionsForCurrentUser(User);
        return transactions
            .Select(MapToDto)
            .ToList();
    }

    [HttpGet(nameof(GetPendingTransactions))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<TransactionResponseDto>> GetPendingTransactions()
    {
        var transactions = await transactionService.GetPendingTransactions();
        return transactions
            .Select(MapToDto)
            .ToList();
    }

    [HttpGet(nameof(GetMyBalance))]
    public async Task<PlayerBalanceResponseDto> GetMyBalance()
    {
        return await transactionService.GetBalanceForCurrentUser(User);
    }

    [HttpGet(nameof(GetPlayerBalance))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<PlayerBalanceResponseDto> GetPlayerBalance([FromQuery] string playerId)
    {
        return await transactionService.GetPlayerBalance(playerId);
    }

    [HttpGet(nameof(GetTransactionsHistory))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<List<TransactionResponseDto>> GetTransactionsHistory(
        [FromQuery] string playerId,
        [FromQuery] string? status)
    {
        var transactions = await transactionService.GetTransactionsForPlayer(playerId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();

            transactions = transactions
                .Where(t => !string.IsNullOrEmpty(t.Status) &&
                            t.Status.ToLowerInvariant() == normalized)
                .ToList();
        }
        return transactions
            .Select(MapToDto)
            .ToList();
    }
}
