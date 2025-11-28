using System;
using System.Collections.Generic;

namespace dataccess.Entities;

public partial class Transaction
{
    public string Id { get; set; } = null!;

    public string Playerid { get; set; } = null!;

    public string Mobilepaynumber { get; set; } = null!;

    public int Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime Createdat { get; set; }

    public DateTime? Approvedat { get; set; }

    public DateTime? Deletedat { get; set; }

    public string? Rejectionreason { get; set; }

    public virtual Player Player { get; set; } = null!;
}
