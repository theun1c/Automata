using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class sale
{
    public long id { get; set; }

    public long machine_id { get; set; }

    public long product_id { get; set; }

    public int quantity { get; set; }

    public decimal sale_amount { get; set; }

    public DateTime sale_datetime { get; set; }

    public long payment_method_id { get; set; }

    public virtual vending_machine machine { get; set; } = null!;

    public virtual payment_method payment_method { get; set; } = null!;

    public virtual product product { get; set; } = null!;
}
