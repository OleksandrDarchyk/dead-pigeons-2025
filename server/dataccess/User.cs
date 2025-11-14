namespace dataccess;

public class User
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Passwordhash { get; set; } = default!; 
    public string Salt { get; set; } = default!;
    public string Role { get; set; } = "Player";
    public DateTime Createdat { get; set; } 
}