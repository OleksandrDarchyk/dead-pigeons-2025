using System;

namespace api.Models.Game
{
    public class PlayerGameHistoryItemDto
    {
        // Id of the game (round)
        public string GameId { get; set; } = null!;

        // ISO week number of the round
        public int WeekNumber { get; set; }

        // Year of the round
        public int Year { get; set; }

        // When this game was closed (winning numbers were set)
        public DateTime? GameClosedAt { get; set; }

        // Id of the board that belongs to this player in that game
        public string BoardId { get; set; } = null!;

        // Numbers that the player chose on this board
        public int[] Numbers { get; set; } = Array.Empty<int>();

        // Price of this board in DKK
        public int Price { get; set; }

        // When the board was created (player bought it)
        public DateTime? BoardCreatedAt { get; set; }

        // Winning numbers for this game (null if not closed yet)
        public int[]? WinningNumbers { get; set; }

        // True if this board is a winner for that game
        public bool IsWinning { get; set; }
    }
}