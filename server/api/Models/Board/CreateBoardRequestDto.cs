using System.ComponentModel.DataAnnotations;

namespace api.Models.Requests;

/// <summary>
/// Request used when a player buys a new board for a specific game.
/// </summary>
public class CreateBoardRequestDto
{
    // Target game for this board (will be validated as active + not closed on the server)
    [Required]
    public string GameId { get; set; } = null!;

    // 5–8 numbers; server will also validate distinct values and range [1..16]
    [Required]
    [MinLength(5)]
    [MaxLength(8)]
    public int[] Numbers { get; set; } = Array.Empty<int>();

    // How many FUTURE games this board should automatically repeat for.
    // 0 = only this game (no auto-repeat).
    //
    // Important:
    // - This value does NOT prepay future games.
    // - The server will create a new board for each future game
    //   and will charge the weekly price at that time,
    //   if the player still has enough balance.
    [Range(0, 52)]
    public int RepeatWeeks { get; set; }
}