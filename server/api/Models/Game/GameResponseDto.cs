namespace api.Models.Responses;

public class GameResponseDto
{ 
    public string Id { get; set; } = null!;
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public int[]? WinningNumbers { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}