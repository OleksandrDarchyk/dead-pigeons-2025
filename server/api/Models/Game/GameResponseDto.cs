namespace api.Models.Responses;

public class GameResponseDto
{
    // Game Id (GUID)
    public string Id { get; set; } = null!;

    // Week number in the year (e.g. 45)
    public int WeekNumber { get; set; }

    // Year (e.g. 2025)
    public int Year { get; set; }

    // Winning numbers (3 numbers or null if not set yet)
    public int[]? WinningNumbers { get; set; }

    // True if this game is currently active
    public bool IsActive { get; set; }

    // When the game was created (seeded)
    public DateTime? CreatedAt { get; set; }

    // When the winning numbers were set (game closed)
    public DateTime? ClosedAt { get; set; }
}