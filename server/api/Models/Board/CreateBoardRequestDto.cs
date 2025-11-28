using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class CreateBoardRequestDto
{
    // Target game for this board (will be validated as active + not closed on the server)
    [Required]
    public string GameId { get; set; } = null!;

    // 5–8 numbers; server will also validate distinct values and range [1..16]
    [Required]
    [MinLength(5)]
    [MaxLength(8)]
    public int[] Numbers { get; set; } = Array.Empty<int>();

    // How many future weeks this board should be reused (0 = only this week)
    // Upper bound keeps the input realistic and prevents nonsense values
    [Range(0, 52)]
    public int RepeatWeeks { get; set; }
}