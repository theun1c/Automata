using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class company
{
    public long id { get; set; }

    public long? parent_company_id { get; set; }

    public string name { get; set; } = null!;

    public string address { get; set; } = null!;

    public string contacts { get; set; } = null!;

    public string? notes { get; set; }

    public DateOnly active_from { get; set; }

    public bool is_active { get; set; }

    public bool is_deleted { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<company> Inverseparent_company { get; set; } = new List<company>();

    public virtual ICollection<franchise_code> franchise_codes { get; set; } = new List<franchise_code>();

    public virtual company? parent_company { get; set; }

    public virtual ICollection<user> users { get; set; } = new List<user>();

    public virtual ICollection<vending_machine> vending_machineclient_companies { get; set; } = new List<vending_machine>();

    public virtual ICollection<vending_machine> vending_machinecompanies { get; set; } = new List<vending_machine>();
}
