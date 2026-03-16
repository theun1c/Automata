using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class franchise_code
{
    public long id { get; set; }

    public long? company_id { get; set; }

    public string code_hash { get; set; } = null!;

    public bool is_active { get; set; }

    public DateTime? expires_at { get; set; }

    public long? created_by_user_id { get; set; }

    public DateTime created_at { get; set; }

    public virtual company? company { get; set; }

    public virtual user? created_by_user { get; set; }
}
