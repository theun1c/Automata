using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class machine_monitor_snapshot
{
    public long machine_id { get; set; }

    public string connection_state { get; set; } = null!;

    public long? connection_type_id { get; set; }

    public int total_load_percent { get; set; }

    public int min_load_percent { get; set; }

    public decimal cash_total { get; set; }

    public decimal coin_sum { get; set; }

    public decimal bill_sum { get; set; }

    public decimal change_sum { get; set; }

    public DateTime? last_ping_at { get; set; }

    public DateTime? last_sale_at { get; set; }

    public DateTime? last_collection_at { get; set; }

    public DateTime? last_service_at { get; set; }

    public decimal sales_today_amount { get; set; }

    public decimal sales_since_service_amount { get; set; }

    public int sales_since_service_count { get; set; }

    public string additional_statuses_json { get; set; } = null!;

    public DateTime updated_at { get; set; }

    public virtual connection_type? connection_type { get; set; }

    public virtual vending_machine machine { get; set; } = null!;
}
