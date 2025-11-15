using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class UpdatePlayerRequestDto
{
    // Player Id (GUID) to identify which player we update
    [Required]
    public string Id { get; set; } = null!;

    // New full name (or same as before)
    [Required]
    [MinLength(3)]
    public string FullName { get; set; } = null!;

    // Optional new email (null means "do not change")
    [EmailAddress]
    public string? Email { get; set; }

    // New phone number (or same as before)
    [Required]
    [MinLength(5)]
    public string Phone { get; set; } = null!;
}