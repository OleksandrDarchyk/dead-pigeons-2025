namespace api.Models.Game;

public class GameResultSummaryDto
{
    public string GameId { get; set; } = null!;
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public int[] WinningNumbers { get; set; } = Array.Empty<int>();
    public int TotalBoards { get; set; }
    public int WinningBoards { get; set; }
    public int DigitalRevenue { get; set; }
}