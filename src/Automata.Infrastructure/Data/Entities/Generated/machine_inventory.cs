using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class machine_inventory
{
    public long machine_id { get; set; }

    public long product_id { get; set; }

    public int quantity { get; set; }

    public int min_stock { get; set; }

    public decimal avg_daily_sales { get; set; }

    public virtual vending_machine machine { get; set; } = null!;

    public virtual product product { get; set; } = null!;
}
