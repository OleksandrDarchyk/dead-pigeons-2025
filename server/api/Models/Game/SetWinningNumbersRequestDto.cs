using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class SetWinningNumbersRequestDto
{
    [Required]
    public string GameId { get; set; } = null!;
    
    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public int[] WinningNumbers { get; set; } = null!;
}