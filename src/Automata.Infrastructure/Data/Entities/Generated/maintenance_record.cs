using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class maintenance_record
{
    public long id { get; set; }

    public long machine_id { get; set; }

    public DateTime service_datetime { get; set; }

    public string work_description { get; set; } = null!;

    public string? issues { get; set; }

    public long? performer_user_id { get; set; }

    public string? performer_name_snapshot { get; set; }

    public DateTime created_at { get; set; }

    public virtual vending_machine machine { get; set; } = null!;

    public virtual user? performer_user { get; set; }
}
