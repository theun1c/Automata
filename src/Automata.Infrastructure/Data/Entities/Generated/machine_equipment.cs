using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class machine_equipment
{
    public long machine_id { get; set; }

    public long equipment_id { get; set; }

    public string status_code { get; set; } = null!;

    public DateTime updated_at { get; set; }

    public virtual equipment_catalog equipment { get; set; } = null!;

    public virtual vending_machine machine { get; set; } = null!;
}
