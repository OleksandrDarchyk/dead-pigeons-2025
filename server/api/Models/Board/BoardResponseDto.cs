// api/Models/Board/BoardResponseDto.cs
using System;

namespace api.Models.Board
{
    public class BoardResponseDto
    {
        public string Id { get; set; } = null!;
        public string PlayerId { get; set; } = null!;
        public string GameId { get; set; } = null!;

        public int[] Numbers { get; set; } = Array.Empty<int>();
        public int Price { get; set; }
        public bool IsWinning { get; set; }
        public int RepeatWeeks { get; set; }
        public bool RepeatActive { get; set; }
        public DateTime? CreatedAt { get; set; }

        // For history / filtering in the UI
        public int GameWeek { get; set; }
        public int GameYear { get; set; }

        // Optional, but very useful for UI labels: Active / Finished, closed date
        public bool GameIsActive { get; set; }
        public DateTime? GameClosedAt { get; set; }
    }
}