using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class product
{
    public long id { get; set; }

    public string name { get; set; } = null!;

    public string? description { get; set; }

    public decimal price { get; set; }

    public bool is_active { get; set; }

    public virtual ICollection<machine_inventory> machine_inventories { get; set; } = new List<machine_inventory>();

    public virtual ICollection<sale> sales { get; set; } = new List<sale>();
}
