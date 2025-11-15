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
    [MinLength(5)] // simple check, we don't do real phone validation here
    public string Phone { get; set; } = null!;
}