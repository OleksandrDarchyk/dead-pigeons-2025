using System;
using System.Collections.Generic;

namespace dataccess.Entities;

public partial class Game
{
    public string Id { get; set; } = null!;

    public int Weeknumber { get; set; }

    public int Year { get; set; }

    public List<int>? Winningnumbers { get; set; }

    public bool Isactive { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Closedat { get; set; }

    public DateTime? Deletedat { get; set; }

    public virtual ICollection<Board> Boards { get; set; } = new List<Board>();
}
