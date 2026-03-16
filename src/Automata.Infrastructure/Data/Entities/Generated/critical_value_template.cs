using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class critical_value_template
{
    public long id { get; set; }

    public string name { get; set; } = null!;

    public string settings_json { get; set; } = null!;

    public virtual ICollection<vending_machine> vending_machines { get; set; } = new List<vending_machine>();
}
