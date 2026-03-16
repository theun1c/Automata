using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class product_matrix
{
    public long id { get; set; }

    public string name { get; set; } = null!;

    public string? description { get; set; }

    public virtual ICollection<vending_machine> vending_machines { get; set; } = new List<vending_machine>();
}
