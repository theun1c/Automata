using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class modem
{
    public long id { get; set; }

    public long provider_id { get; set; }

    public string serial_number { get; set; } = null!;

    public long? connection_type_id { get; set; }

    public bool is_active { get; set; }

    public DateTime? last_ping_at { get; set; }

    public DateTime created_at { get; set; }

    public virtual connection_type? connection_type { get; set; }

    public virtual provider provider { get; set; } = null!;

    public virtual vending_machine? vending_machine { get; set; }
}
