using System;

namespace api.Models.Game
{
    public class PlayerGameHistoryItemDto
    {
        public string GameId { get; set; } = null!;
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public DateTime? GameClosedAt { get; set; }
        public string BoardId { get; set; } = null!;
        public int[] Numbers { get; set; } = Array.Empty<int>();
        public int Price { get; set; }
        public DateTime? BoardCreatedAt { get; set; }
        public int[]? WinningNumbers { get; set; }
        public bool IsWinning { get; set; }
    }
}