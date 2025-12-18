using System.ComponentModel.DataAnnotations;

namespace api.Models.Transactions;

public class CreateTransactionForCurrentUserRequestDto
{
    [Required]
    [MinLength(3)]
    public string MobilePayNumber { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Amount { get; set; }
}
