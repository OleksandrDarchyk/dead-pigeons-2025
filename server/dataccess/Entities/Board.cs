using System;
using System.Collections.Generic;

namespace dataccess.Entities;

public partial class Board
{
    public string Id { get; set; } = null!;

    public string? Playerid { get; set; }

    public string? Gameid { get; set; }

    public List<int> Numbers { get; set; } = null!;

    public int Price { get; set; }

    public bool Iswinning { get; set; }

    public int Repeatweeks { get; set; }

    public bool Repeatactive { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Deletedat { get; set; }

    public virtual Game? Game { get; set; }

    public virtual Player? Player { get; set; }
}
