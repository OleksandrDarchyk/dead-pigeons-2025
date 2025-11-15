using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class SetWinningNumbersRequestDto
{
    // Game Id that we want to close
    [Required]
    public string GameId { get; set; } = null!;

    // Exactly 3 winning numbers from 1 to 16
    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public int[] WinningNumbers { get; set; } = null!;
}