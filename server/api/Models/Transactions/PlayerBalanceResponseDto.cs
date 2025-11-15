namespace api.Models.Responses;

public class PlayerBalanceResponseDto
{
    public string PlayerId { get; set; } = null!;

    // Calculated balance:
    // sum(Approved transactions.amount) - sum(boards.price)
    public int Balance { get; set; }
}