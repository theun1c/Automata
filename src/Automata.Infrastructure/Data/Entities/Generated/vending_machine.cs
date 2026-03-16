using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class vending_machine
{
    public long id { get; set; }

    public long company_id { get; set; }

    public string name { get; set; } = null!;

    public long manufacturer_id { get; set; }

    public long model_id { get; set; }

    public long? slave_manufacturer_id { get; set; }

    public long? slave_model_id { get; set; }

    public string work_mode { get; set; } = null!;

    public long status_id { get; set; }

    public string address { get; set; } = null!;

    public string place_description { get; set; } = null!;

    public decimal? latitude { get; set; }

    public decimal? longitude { get; set; }

    public string machine_number { get; set; } = null!;

    public string work_time_text { get; set; } = null!;

    public long timezone_id { get; set; }

    public long product_matrix_id { get; set; }

    public long critical_value_template_id { get; set; }

    public long notification_template_id { get; set; }

    public long? client_company_id { get; set; }

    public long? manager_user_id { get; set; }

    public long? engineer_user_id { get; set; }

    public long? technician_user_id { get; set; }

    public long? rfid_service_card_id { get; set; }

    public long? rfid_collection_card_id { get; set; }

    public long? rfid_loading_card_id { get; set; }

    public string? kit_online_cashbox_id { get; set; }

    public long service_priority_id { get; set; }

    public long? modem_id { get; set; }

    public string? notes { get; set; }

    public DateTime installed_at { get; set; }

    public DateTime? last_service_at { get; set; }

    public decimal total_income { get; set; }

    public bool is_active { get; set; }

    public bool is_deleted { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual company? client_company { get; set; }

    public virtual company company { get; set; } = null!;

    public virtual critical_value_template critical_value_template { get; set; } = null!;

    public virtual user? engineer_user { get; set; }

    public virtual ICollection<machine_equipment> machine_equipments { get; set; } = new List<machine_equipment>();

    public virtual ICollection<machine_event> machine_events { get; set; } = new List<machine_event>();

    public virtual ICollection<machine_inventory> machine_inventories { get; set; } = new List<machine_inventory>();

    public virtual machine_monitor_snapshot? machine_monitor_snapshot { get; set; }

    public virtual ICollection<machine_payment_system> machine_payment_systems { get; set; } = new List<machine_payment_system>();

    public virtual ICollection<maintenance_record> maintenance_records { get; set; } = new List<maintenance_record>();

    public virtual user? manager_user { get; set; }

    public virtual manufacturer manufacturer { get; set; } = null!;

    public virtual machine_model model { get; set; } = null!;

    public virtual modem? modem { get; set; }

    public virtual notification_template notification_template { get; set; } = null!;

    public virtual product_matrix product_matrix { get; set; } = null!;

    public virtual rfid_card? rfid_collection_card { get; set; }

    public virtual rfid_card? rfid_loading_card { get; set; }

    public virtual rfid_card? rfid_service_card { get; set; }

    public virtual ICollection<sale> sales { get; set; } = new List<sale>();

    public virtual service_priority service_priority { get; set; } = null!;

    public virtual manufacturer? slave_manufacturer { get; set; }

    public virtual machine_model? slave_model { get; set; }

    public virtual machine_status status { get; set; } = null!;

    public virtual user? technician_user { get; set; }

    public virtual timezone timezone { get; set; } = null!;
}
