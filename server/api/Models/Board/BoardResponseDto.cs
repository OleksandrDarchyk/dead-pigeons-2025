namespace api.Models.Responses;

public class BoardResponseDto
{
    public string Id { get; set; } = null!;

    public string PlayerId { get; set; } = null!;

    public string GameId { get; set; } = null!;

    // Sorted list of numbers on the board
    public int[] Numbers { get; set; } = null!;

    // Price paid for this board (DKK)
    public int Price { get; set; }

    // True if this board is a winning board
    public bool IsWinning { get; set; }

    public int RepeatWeeks { get; set; }

    public bool RepeatActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}