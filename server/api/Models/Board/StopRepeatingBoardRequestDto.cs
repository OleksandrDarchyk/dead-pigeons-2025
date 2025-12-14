using System.ComponentModel.DataAnnotations;

public class StopRepeatingBoardRequestDto
{
    [Required]
    public string BoardId { get; set; } = null!;
}