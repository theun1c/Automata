using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class user
{
    public long id { get; set; }

    public long? company_id { get; set; }

    public long role_id { get; set; }

    public string full_name { get; set; } = null!;

    public string email { get; set; } = null!;

    public string? phone { get; set; }

    public string password_hash { get; set; } = null!;

    public string? photo_path { get; set; }

    public bool is_email_confirmed { get; set; }

    public bool is_active { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual company? company { get; set; }

    public virtual ICollection<franchise_code> franchise_codes { get; set; } = new List<franchise_code>();

    public virtual ICollection<maintenance_record> maintenance_records { get; set; } = new List<maintenance_record>();

    public virtual role role { get; set; } = null!;

    public virtual ICollection<vending_machine> vending_machineengineer_users { get; set; } = new List<vending_machine>();

    public virtual ICollection<vending_machine> vending_machinemanager_users { get; set; } = new List<vending_machine>();

    public virtual ICollection<vending_machine> vending_machinetechnician_users { get; set; } = new List<vending_machine>();
}
