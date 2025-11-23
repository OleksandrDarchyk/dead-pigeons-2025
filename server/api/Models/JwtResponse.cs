namespace api.Models;

public class JwtResponse
{
    public string Token { get; set; } = string.Empty;

    public JwtResponse()
    {
    }

    public JwtResponse(string token)
    {
        Token = token;
    }
}