using System;
using System.Collections.Generic;

namespace dataccess.Entities;

public partial class User
{
    public string Id { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? Createdat { get; set; }

    public DateTime? Deletedat { get; set; }
}
