using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class manufacturer
{
    public long id { get; set; }

    public string name { get; set; } = null!;

    public virtual ICollection<machine_model> machine_models { get; set; } = new List<machine_model>();

    public virtual ICollection<vending_machine> vending_machinemanufacturers { get; set; } = new List<vending_machine>();

    public virtual ICollection<vending_machine> vending_machineslave_manufacturers { get; set; } = new List<vending_machine>();
}
