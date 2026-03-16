using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class machine_event
{
    public long id { get; set; }

    public long machine_id { get; set; }

    public string event_type { get; set; } = null!;

    public string message { get; set; } = null!;

    public string severity { get; set; } = null!;

    public DateTime occurred_at { get; set; }

    public virtual vending_machine machine { get; set; } = null!;
}
