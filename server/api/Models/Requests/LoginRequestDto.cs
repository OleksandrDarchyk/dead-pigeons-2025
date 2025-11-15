namespace api.Models.Requests;

public class LoginRequestDto
{
    // Email entered by user in login form
    public string Email { get; set; } = null!;

    // Plain password from login form (will be hashed on server)
    public string Password { get; set; } = null!;
}