using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class machine_payment_system
{
    public long machine_id { get; set; }

    public string payment_system_code { get; set; } = null!;

    public virtual vending_machine machine { get; set; } = null!;
}
