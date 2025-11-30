//delete before exam 
//we dont have id becouse id is GUID and creates automatically by logic 

using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

public class CreatePlayerRequestDto
{
    [Required]
    [MinLength(3)]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [RegularExpression(@"^[0-9+\-\s]{4,30}$", 
        ErrorMessage = "Phone number can only contain digits, +, - and spaces.")]
    public string Phone { get; set; } = null!;

}