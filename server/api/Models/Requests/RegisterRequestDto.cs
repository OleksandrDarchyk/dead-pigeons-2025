// api/Models/Requests/RegisterRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class RegisterRequestDto
{
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    // Raw password, server will hash it
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;

    // Simple confirm password check on the server
    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = null!;
}