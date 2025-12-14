namespace api.Configuration;

/// <summary>
/// Configuration options for one-time admin bootstrap in Production.
/// Values should come from environment variables / secrets, not from committed appsettings.json.
/// </summary>
public class AdminBootstrapOptions
{
    /// <summary>
    /// Email for the initial admin user (for example: admin@deadpigeons.dk).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Plain-text password for the initial admin user.
    /// This should only be provided via secrets in Production and removed after the admin is created.
    /// </summary>
    public string? Password { get; set; }
}