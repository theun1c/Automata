using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class email_verification_code
{
    public long id { get; set; }

    public string email { get; set; } = null!;

    public string code { get; set; } = null!;

    public DateTime expires_at { get; set; }

    public DateTime? used_at { get; set; }

    public int attempts { get; set; }

    public DateTime created_at { get; set; }
}
