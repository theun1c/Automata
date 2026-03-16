using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class machine_model
{
    public long id { get; set; }

    public long manufacturer_id { get; set; }

    public string name { get; set; } = null!;

    public virtual manufacturer manufacturer { get; set; } = null!;

    public virtual ICollection<vending_machine> vending_machinemodels { get; set; } = new List<vending_machine>();

    public virtual ICollection<vending_machine> vending_machineslave_models { get; set; } = new List<vending_machine>();
}
