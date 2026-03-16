using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class equipment_catalog
{
    public long id { get; set; }

    public string code { get; set; } = null!;

    public string name { get; set; } = null!;

    public virtual ICollection<machine_equipment> machine_equipments { get; set; } = new List<machine_equipment>();
}
