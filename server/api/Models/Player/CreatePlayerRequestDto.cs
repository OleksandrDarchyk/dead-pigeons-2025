using System.ComponentModel.DataAnnotations;

public class CreatePlayerRequestDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [MinLength(3, ErrorMessage = "Full name must be at least 3 characters.")]
    [MaxLength(100, ErrorMessage = "Full name cannot be longer than 100 characters.")]
    public string FullName { get; set; } = default!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[0-9+\-\s]{6,20}$",
        ErrorMessage = "Phone number can contain only digits, spaces, '+' and '-' and must be 6–20 characters.")]
    public string Phone { get; set; } = default!;
}