using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class CreateBoardRequestDto
{
    // Player who buys this board
    [Required]
    public string PlayerId { get; set; } = null!;

    // Game to join
    [Required]
    public string GameId { get; set; } = null!;

    // Numbers chosen by the player (5–8 distinct numbers between 1 and 16)
    [Required]
    [MinLength(5)]
    [MaxLength(8)]
    public int[] Numbers { get; set; } = null!;

    // How many future weeks to repeat this board for (0 = do not repeat)
    [Range(0, int.MaxValue)]
    public int RepeatWeeks { get; set; }
}