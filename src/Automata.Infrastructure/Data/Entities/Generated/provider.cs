using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class provider
{
    public long id { get; set; }

    public string name { get; set; } = null!;

    public virtual ICollection<modem> modems { get; set; } = new List<modem>();
}
