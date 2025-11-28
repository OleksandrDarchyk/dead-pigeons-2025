using System.ComponentModel.DataAnnotations;

namespace api.Models.Transactions;

public class CreateTransactionForCurrentUserRequestDto
{
    // MobilePay transaction number written by the player
    [Required]
    [MinLength(3)]
    public string MobilePayNumber { get; set; } = null!;

    // Amount in DKK - must be positive
    [Range(1, int.MaxValue)]
    public int Amount { get; set; }
}
