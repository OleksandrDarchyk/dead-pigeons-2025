using System;

namespace api.Models.Board
{
    public class BoardResponseDto
    {
        public string Id { get; set; } = null!;
        public string PlayerId { get; set; } = null!;
        public string GameId { get; set; } = null!;
        public int[] Numbers { get; set; } = null!;
        public int Price { get; set; }
        public bool IsWinning { get; set; }
        public int RepeatWeeks { get; set; }
        public bool RepeatActive { get; set; }
        public DateTime? CreatedAt { get; set; }

        // NEW: for easier UI, no extra calls
        public int GameWeek { get; set; }
        public int GameYear { get; set; }
    }

}