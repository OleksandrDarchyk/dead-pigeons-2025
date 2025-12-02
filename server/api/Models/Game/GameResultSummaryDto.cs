namespace api.Models.Game;

public class GameResultSummaryDto
{
    // Id of the game that was just closed
    public string GameId { get; set; } = null!;

    // ISO week and year of that game
    public int WeekNumber { get; set; }
    public int Year { get; set; }

    // The 3 winning numbers (sorted ascending)
    public int[] WinningNumbers { get; set; } = Array.Empty<int>();

    // How many non-deleted boards were sold for this game
    public int TotalBoards { get; set; }

    // How many boards are marked as winning
    public int WinningBoards { get; set; }

    // Sum of all board prices for this game (digital revenue before 70/30 split)
    public int DigitalRevenue { get; set; }
}