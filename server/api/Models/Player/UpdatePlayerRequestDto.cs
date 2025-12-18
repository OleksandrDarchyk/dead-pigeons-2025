using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class UpdatePlayerRequestDto
{
    [Required]
    public string Id { get; set; } = null!;

    [Required]
    [MinLength(3)]
    public string FullName { get; set; } = null!;

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MinLength(5)]
    public string Phone { get; set; } = null!;
}