namespace api.Configuration;

// Settings used to create the very first admin account in Production.

public class AdminBootstrapOptions
{
    /// Admin email address.
    public string? Email { get; set; }
    
    /// Admin password in plain text. Use only for the initial bootstrap, then remove/rotate.
    public string? Password { get; set; }
}