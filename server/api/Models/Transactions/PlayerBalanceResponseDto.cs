namespace api.Models.Responses;

public class PlayerBalanceResponseDto
{
    public string PlayerId { get; set; } = null!;
    public int Balance { get; set; }
}