using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class connection_type
{
    public long id { get; set; }

    public string code { get; set; } = null!;

    public string name { get; set; } = null!;

    public virtual ICollection<machine_monitor_snapshot> machine_monitor_snapshots { get; set; } = new List<machine_monitor_snapshot>();

    public virtual ICollection<modem> modems { get; set; } = new List<modem>();
}
