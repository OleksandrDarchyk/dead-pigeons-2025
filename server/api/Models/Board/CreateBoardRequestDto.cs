using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;
public class CreateBoardRequestDto
{
    [Required]
    public string GameId { get; set; } = null!;
    
    [Required]
    [MinLength(5)]
    [MaxLength(8)]
    public int[] Numbers { get; set; } = Array.Empty<int>();
    
    [Range(0, 52)]
    public int RepeatWeeks { get; set; }
}