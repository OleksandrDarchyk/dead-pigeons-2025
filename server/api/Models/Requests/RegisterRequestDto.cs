using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class RegisterRequestDto
{
    // Email for login, must be valid format
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    // Raw password, server will hash+salt it
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;
}