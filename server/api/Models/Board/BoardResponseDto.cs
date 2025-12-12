using System;

namespace api.Models.Board
{
    /// <summary>
    /// Board data returned to the client (admin or player).
    /// </summary>
    public class BoardResponseDto
    {
        // Board Id (GUID string)
        public string Id { get; set; } = null!;

        // Owner of this board
        public string PlayerId { get; set; } = null!;

        // Game (round) this board belongs to
        public string GameId { get; set; } = null!;

        // Numbers chosen by the player (5–8 numbers in range [1..16])
        public int[] Numbers { get; set; } = Array.Empty<int>();

        // Price for ONE game (one week) in DKK.
        // Important:
        // - This is always the weekly price (20 / 40 / 80 / 160).
        // - We do NOT store "prepaid multi-week price" here.
        public int Price { get; set; }

        // True if this board is a winner for its game
        public bool IsWinning { get; set; }

        // Remaining number of FUTURE games this board should auto-repeat for
        // (from the perspective of this game).
        // Example:
        // - If RepeatWeeks = 2, this board should still be created
        //   for the next 2 future games (if repeat is active and balance is OK).
        public int RepeatWeeks { get; set; }

        // Indicates whether auto-repeat is currently enabled for this board.
        // When the player chooses "stop repeating", the server will set this to false.
        public bool RepeatActive { get; set; }

        // When the board was created (player bought it)
        public DateTime? CreatedAt { get; set; }

        // ==========================
        // Game metadata (for UI)
        // ==========================

        // ISO week number of the game this board belongs to
        public int GameWeek { get; set; }

        // Year of the game
        public int GameYear { get; set; }

        // True if the game is still active (no winning numbers yet)
        public bool GameIsActive { get; set; }

        // When the game was closed (winning numbers were set)
        public DateTime? GameClosedAt { get; set; }
    }
}
