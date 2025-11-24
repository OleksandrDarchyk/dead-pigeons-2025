using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class CreateBoardRequestDto
{
    // Game the board belongs to
    [Required]
    public string GameId { get; set; } = null!;

    // 5–8 distinct numbers between 1 and 16
    [Required]
    [MinLength(5)]
    [MaxLength(8)]
    public int[] Numbers { get; set; } = Array.Empty<int>();

    // How many future weeks to reuse this board (0 = only this week)
    [Range(0, int.MaxValue)]
    public int RepeatWeeks { get; set; }
}