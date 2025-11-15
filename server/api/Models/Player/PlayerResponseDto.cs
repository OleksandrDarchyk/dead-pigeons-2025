namespace api.Models.Responses;

public class PlayerResponseDto
{
    // Player Id (GUID string)
    public string Id { get; set; } = null!;

    // Full name (maps from entity.Fullname)
    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    // Business rule: only active players can buy boards
    public bool IsActive { get; set; }

    // When the player was activated (null if never activated)
    public DateTime? ActivatedAt { get; set; }

    // When the player was created (for history)
    public DateTime? CreatedAt { get; set; }
}