namespace api.Models.Responses;

public class TransactionResponseDto
{
    public string Id { get; set; } = null!;

    public string PlayerId { get; set; } = null!;

    public string MobilePayNumber { get; set; } = null!;

    public int Amount { get; set; }

    // "Pending", "Approved", "Rejected", etc.
    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }
}