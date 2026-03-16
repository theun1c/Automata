using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class role
{
    public long id { get; set; }

    public string code { get; set; } = null!;

    public string name { get; set; } = null!;

    public virtual ICollection<user> users { get; set; } = new List<user>();
}
