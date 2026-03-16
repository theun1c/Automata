using System;
using System.Collections.Generic;
using Automata.Infrastructure.Data.Entities.Generated;
using Microsoft.EntityFrameworkCore;

namespace Automata.Infrastructure.Data;

public partial class AutomataDbContext : DbContext
{
    public AutomataDbContext(DbContextOptions<AutomataDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<company> companies { get; set; }

    public virtual DbSet<connection_type> connection_types { get; set; }

    public virtual DbSet<critical_value_template> critical_value_templates { get; set; }

    public virtual DbSet<email_verification_code> email_verification_codes { get; set; }

    public virtual DbSet<equipment_catalog> equipment_catalogs { get; set; }

    public virtual DbSet<franchise_code> franchise_codes { get; set; }

    public virtual DbSet<machine_equipment> machine_equipments { get; set; }

    public virtual DbSet<machine_event> machine_events { get; set; }

    public virtual DbSet<machine_inventory> machine_inventories { get; set; }

    public virtual DbSet<machine_model> machine_models { get; set; }

    public virtual DbSet<machine_monitor_snapshot> machine_monitor_snapshots { get; set; }

    public virtual DbSet<machine_payment_system> machine_payment_systems { get; set; }

    public virtual DbSet<machine_status> machine_statuses { get; set; }

    public virtual DbSet<maintenance_record> maintenance_records { get; set; }

    public virtual DbSet<manufacturer> manufacturers { get; set; }

    public virtual DbSet<modem> modems { get; set; }

    public virtual DbSet<news_item> news_items { get; set; }

    public virtual DbSet<notification_template> notification_templates { get; set; }

    public virtual DbSet<payment_method> payment_methods { get; set; }

    public virtual DbSet<product> products { get; set; }

    public virtual DbSet<product_matrix> product_matrices { get; set; }

    public virtual DbSet<provider> providers { get; set; }

    public virtual DbSet<rfid_card> rfid_cards { get; set; }

    public virtual DbSet<role> roles { get; set; }

    public virtual DbSet<sale> sales { get; set; }

    public virtual DbSet<service_priority> service_priorities { get; set; }

    public virtual DbSet<timezone> timezones { get; set; }

    public virtual DbSet<user> users { get; set; }

    public virtual DbSet<vending_machine> vending_machines { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<company>(entity =>
        {
            entity.HasKey(e => e.id).HasName("companies_pkey");

            entity.ToTable("companies", "automata");

            entity.Property(e => e.id).HasDefaultValueSql("nextval('companies_id_seq'::regclass)");
            entity.Property(e => e.active_from).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.name).HasMaxLength(255);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.parent_company).WithMany(p => p.Inverseparent_company)
                .HasForeignKey(d => d.parent_company_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("companies_parent_company_id_fkey");
        });

        modelBuilder.Entity<connection_type>(entity =>
        {
            entity.HasKey(e => e.id).HasName("connection_types_pkey");

            entity.ToTable("connection_types", "automata");

            entity.HasIndex(e => e.code, "connection_types_code_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('connection_types_id_seq'::regclass)");
            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.name).HasMaxLength(100);
        });

        modelBuilder.Entity<critical_value_template>(entity =>
        {
            entity.HasKey(e => e.id).HasName("critical_value_templates_pkey");

            entity.ToTable("critical_value_templates", "automata");

            entity.HasIndex(e => e.name, "critical_value_templates_name_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('critical_value_templates_id_seq'::regclass)");
            entity.Property(e => e.name).HasMaxLength(200);
            entity.Property(e => e.settings_json)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<email_verification_code>(entity =>
        {
            entity.HasKey(e => e.id).HasName("email_verification_codes_pkey");

            entity.ToTable("email_verification_codes", "automata");

            entity.Property(e => e.id).HasDefaultValueSql("nextval('email_verification_codes_id_seq'::regclass)");
            entity.Property(e => e.attempts).HasDefaultValue(0);
            entity.Property(e => e.code).HasMaxLength(12);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.email).HasMaxLength(320);
        });

        modelBuilder.Entity<equipment_catalog>(entity =>
        {
            entity.HasKey(e => e.id).HasName("equipment_catalog_pkey");

            entity.ToTable("equipment_catalog", "automata");

            entity.HasIndex(e => e.code, "equipment_catalog_code_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('equipment_catalog_id_seq'::regclass)");
            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.name).HasMaxLength(200);
        });

        modelBuilder.Entity<franchise_code>(entity =>
        {
            entity.HasKey(e => e.id).HasName("franchise_codes_pkey");

            entity.ToTable("franchise_codes", "automata");

            entity.Property(e => e.id).HasDefaultValueSql("nextval('franchise_codes_id_seq'::regclass)");
            entity.Property(e => e.code_hash).HasMaxLength(255);
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);

            entity.HasOne(d => d.company).WithMany(p => p.franchise_codes)
                .HasForeignKey(d => d.company_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("franchise_codes_company_id_fkey");

            entity.HasOne(d => d.created_by_user).WithMany(p => p.franchise_codes)
                .HasForeignKey(d => d.created_by_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("franchise_codes_created_by_user_id_fkey");
        });

        modelBuilder.Entity<machine_equipment>(entity =>
        {
            entity.HasKey(e => new { e.machine_id, e.equipment_id }).HasName("machine_equipment_pkey");

            entity.ToTable("machine_equipment", "automata");

            entity.Property(e => e.status_code).HasMaxLength(50);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.equipment).WithMany(p => p.machine_equipments)
                .HasForeignKey(d => d.equipment_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("machine_equipment_equipment_id_fkey");

            entity.HasOne(d => d.machine).WithMany(p => p.machine_equipments)
                .HasForeignKey(d => d.machine_id)
                .HasConstraintName("machine_equipment_machine_id_fkey");
        });

        modelBuilder.Entity<machine_event>(entity =>
        {
            entity.HasKey(e => e.id).HasName("machine_events_pkey");

            entity.ToTable("machine_events", "automata");

            entity.HasIndex(e => new { e.machine_id, e.occurred_at }, "ix_machine_events_machine_time").IsDescending(false, true);

            entity.Property(e => e.id).HasDefaultValueSql("nextval('machine_events_id_seq'::regclass)");
            entity.Property(e => e.event_type).HasMaxLength(100);
            entity.Property(e => e.severity).HasMaxLength(20);

            entity.HasOne(d => d.machine).WithMany(p => p.machine_events)
                .HasForeignKey(d => d.machine_id)
                .HasConstraintName("machine_events_machine_id_fkey");
        });

        modelBuilder.Entity<machine_inventory>(entity =>
        {
            entity.HasKey(e => new { e.machine_id, e.product_id }).HasName("machine_inventory_pkey");

            entity.ToTable("machine_inventory", "automata");

            entity.Property(e => e.avg_daily_sales).HasPrecision(10, 2);
            entity.Property(e => e.min_stock).HasDefaultValue(0);
            entity.Property(e => e.quantity).HasDefaultValue(0);

            entity.HasOne(d => d.machine).WithMany(p => p.machine_inventories)
                .HasForeignKey(d => d.machine_id)
                .HasConstraintName("machine_inventory_machine_id_fkey");

            entity.HasOne(d => d.product).WithMany(p => p.machine_inventories)
                .HasForeignKey(d => d.product_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("machine_inventory_product_id_fkey");
        });

        modelBuilder.Entity<machine_model>(entity =>
        {
            entity.HasKey(e => e.id).HasName("machine_models_pkey");

            entity.ToTable("machine_models", "automata");

            entity.HasIndex(e => new { e.manufacturer_id, e.name }, "uq_machine_models").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('machine_models_id_seq'::regclass)");
            entity.Property(e => e.name).HasMaxLength(200);

            entity.HasOne(d => d.manufacturer).WithMany(p => p.machine_models)
                .HasForeignKey(d => d.manufacturer_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("machine_models_manufacturer_id_fkey");
        });

        modelBuilder.Entity<machine_monitor_snapshot>(entity =>
        {
            entity.HasKey(e => e.machine_id).HasName("machine_monitor_snapshots_pkey");

            entity.ToTable("machine_monitor_snapshots", "automata");

            entity.HasIndex(e => e.connection_state, "ix_monitor_connection_state");

            entity.HasIndex(e => e.updated_at, "ix_monitor_updated_at").IsDescending();

            entity.Property(e => e.machine_id).ValueGeneratedNever();
            entity.Property(e => e.additional_statuses_json)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb");
            entity.Property(e => e.bill_sum).HasPrecision(14, 2);
            entity.Property(e => e.cash_total).HasPrecision(14, 2);
            entity.Property(e => e.change_sum).HasPrecision(14, 2);
            entity.Property(e => e.coin_sum).HasPrecision(14, 2);
            entity.Property(e => e.connection_state).HasMaxLength(20);
            entity.Property(e => e.sales_since_service_amount).HasPrecision(14, 2);
            entity.Property(e => e.sales_since_service_count).HasDefaultValue(0);
            entity.Property(e => e.sales_today_amount).HasPrecision(14, 2);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.connection_type).WithMany(p => p.machine_monitor_snapshots)
                .HasForeignKey(d => d.connection_type_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("machine_monitor_snapshots_connection_type_id_fkey");

            entity.HasOne(d => d.machine).WithOne(p => p.machine_monitor_snapshot)
                .HasForeignKey<machine_monitor_snapshot>(d => d.machine_id)
                .HasConstraintName("machine_monitor_snapshots_machine_id_fkey");
        });

        modelBuilder.Entity<machine_payment_system>(entity =>
        {
            entity.HasKey(e => new { e.machine_id, e.payment_system_code }).HasName("machine_payment_systems_pkey");

            entity.ToTable("machine_payment_systems", "automata");

            entity.Property(e => e.payment_system_code).HasMaxLength(50);

            entity.HasOne(d => d.machine).WithMany(p => p.machine_payment_systems)
                .HasForeignKey(d => d.machine_id)
                .HasConstraintName("machine_payment_systems_machine_id_fkey");
        });

        modelBuilder.Entity<machine_status>(entity =>
        {
            entity.HasKey(e => e.id).HasName("machine_statuses_pkey");

            entity.ToTable("machine_statuses", "automata");

            entity.HasIndex(e => e.code, "machine_statuses_code_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('machine_statuses_id_seq'::regclass)");
            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.name).HasMaxLength(100);
        });

        modelBuilder.Entity<maintenance_record>(entity =>
        {
            entity.HasKey(e => e.id).HasName("maintenance_records_pkey");

            entity.ToTable("maintenance_records", "automata");

            entity.HasIndex(e => new { e.machine_id, e.service_datetime }, "ix_maintenance_machine_datetime").IsDescending(false, true);

            entity.Property(e => e.id).HasDefaultValueSql("nextval('maintenance_records_id_seq'::regclass)");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.performer_name_snapshot).HasMaxLength(255);

            entity.HasOne(d => d.machine).WithMany(p => p.maintenance_records)
                .HasForeignKey(d => d.machine_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("maintenance_records_machine_id_fkey");

            entity.HasOne(d => d.performer_user).WithMany(p => p.maintenance_records)
                .HasForeignKey(d => d.performer_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("maintenance_records_performer_user_id_fkey");
        });

        modelBuilder.Entity<manufacturer>(entity =>
        {
            entity.HasKey(e => e.id).HasName("manufacturers_pkey");

            entity.ToTable("manufacturers", "automata");

            entity.HasIndex(e => e.name, "manufacturers_name_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('manufacturers_id_seq'::regclass)");
            entity.Property(e => e.name).HasMaxLength(200);
        });

        modelBuilder.Entity<modem>(entity =>
        {
            entity.HasKey(e => e.id).HasName("modems_pkey");

            entity.ToTable("modems", "automata");

            entity.HasIndex(e => e.serial_number, "modems_serial_number_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('modems_id_seq'::regclass)");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.serial_number).HasMaxLength(100);

            entity.HasOne(d => d.connection_type).WithMany(p => p.modems)
                .HasForeignKey(d => d.connection_type_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("modems_connection_type_id_fkey");

            entity.HasOne(d => d.provider).WithMany(p => p.modems)
                .HasForeignKey(d => d.provider_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("modems_provider_id_fkey");
        });

        modelBuilder.Entity<news_item>(entity =>
        {
            entity.HasKey(e => e.id).HasName("news_items_pkey");

            entity.ToTable("news_items", "automata");

            entity.Property(e => e.id).HasDefaultValueSql("nextval('news_items_id_seq'::regclass)");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.title).HasMaxLength(255);
        });

        modelBuilder.Entity<notification_template>(entity =>
        {
            entity.HasKey(e => e.id).HasName("notification_templates_pkey");

            entity.ToTable("notification_templates", "automata");

            entity.HasIndex(e => e.name, "notification_templates_name_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('notification_templates_id_seq'::regclass)");
            entity.Property(e => e.name).HasMaxLength(200);
            entity.Property(e => e.settings_json)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<payment_method>(entity =>
        {
            entity.HasKey(e => e.id).HasName("payment_methods_pkey");

            entity.ToTable("payment_methods", "automata");

            entity.HasIndex(e => e.code, "payment_methods_code_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('payment_methods_id_seq'::regclass)");
            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.name).HasMaxLength(100);
        });

        modelBuilder.Entity<product>(entity =>
        {
            entity.HasKey(e => e.id).HasName("products_pkey");

            entity.ToTable("products", "automata");

            entity.HasIndex(e => e.name, "products_name_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('products_id_seq'::regclass)");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.name).HasMaxLength(200);
            entity.Property(e => e.price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<product_matrix>(entity =>
        {
            entity.HasKey(e => e.id).HasName("product_matrices_pkey");

            entity.ToTable("product_matrices", "automata");

            entity.HasIndex(e => e.name, "product_matrices_name_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('product_matrices_id_seq'::regclass)");
            entity.Property(e => e.name).HasMaxLength(200);
        });

        modelBuilder.Entity<provider>(entity =>
        {
            entity.HasKey(e => e.id).HasName("providers_pkey");

            entity.ToTable("providers", "automata");

            entity.HasIndex(e => e.name, "providers_name_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('providers_id_seq'::regclass)");
            entity.Property(e => e.name).HasMaxLength(200);
        });

        modelBuilder.Entity<rfid_card>(entity =>
        {
            entity.HasKey(e => e.id).HasName("rfid_cards_pkey");

            entity.ToTable("rfid_cards", "automata");

            entity.HasIndex(e => e.card_number, "rfid_cards_card_number_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('rfid_cards_id_seq'::regclass)");
            entity.Property(e => e.card_number).HasMaxLength(100);
            entity.Property(e => e.card_type).HasMaxLength(20);
        });

        modelBuilder.Entity<role>(entity =>
        {
            entity.HasKey(e => e.id).HasName("roles_pkey");

            entity.ToTable("roles", "automata");

            entity.HasIndex(e => e.code, "roles_code_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('roles_id_seq'::regclass)");
            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.name).HasMaxLength(100);
        });

        modelBuilder.Entity<sale>(entity =>
        {
            entity.HasKey(e => e.id).HasName("sales_pkey");

            entity.ToTable("sales", "automata");

            entity.HasIndex(e => new { e.machine_id, e.sale_datetime }, "ix_sales_machine_datetime").IsDescending(false, true);

            entity.HasIndex(e => e.product_id, "ix_sales_product_id");

            entity.Property(e => e.id).HasDefaultValueSql("nextval('sales_id_seq'::regclass)");
            entity.Property(e => e.sale_amount).HasPrecision(12, 2);

            entity.HasOne(d => d.machine).WithMany(p => p.sales)
                .HasForeignKey(d => d.machine_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sales_machine_id_fkey");

            entity.HasOne(d => d.payment_method).WithMany(p => p.sales)
                .HasForeignKey(d => d.payment_method_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sales_payment_method_id_fkey");

            entity.HasOne(d => d.product).WithMany(p => p.sales)
                .HasForeignKey(d => d.product_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sales_product_id_fkey");
        });

        modelBuilder.Entity<service_priority>(entity =>
        {
            entity.HasKey(e => e.id).HasName("service_priorities_pkey");

            entity.ToTable("service_priorities", "automata");

            entity.HasIndex(e => e.code, "service_priorities_code_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('service_priorities_id_seq'::regclass)");
            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.name).HasMaxLength(100);
        });

        modelBuilder.Entity<timezone>(entity =>
        {
            entity.HasKey(e => e.id).HasName("timezones_pkey");

            entity.ToTable("timezones", "automata");

            entity.HasIndex(e => e.name, "timezones_name_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('timezones_id_seq'::regclass)");
            entity.Property(e => e.name).HasMaxLength(100);
            entity.Property(e => e.utc_offset).HasMaxLength(6);
        });

        modelBuilder.Entity<user>(entity =>
        {
            entity.HasKey(e => e.id).HasName("users_pkey");

            entity.ToTable("users", "automata");

            entity.Property(e => e.id).HasDefaultValueSql("nextval('users_id_seq'::regclass)");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.email).HasMaxLength(320);
            entity.Property(e => e.full_name).HasMaxLength(255);
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_email_confirmed).HasDefaultValue(false);
            entity.Property(e => e.password_hash).HasMaxLength(255);
            entity.Property(e => e.phone).HasMaxLength(32);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");

            entity.HasOne(d => d.company).WithMany(p => p.users)
                .HasForeignKey(d => d.company_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("users_company_id_fkey");

            entity.HasOne(d => d.role).WithMany(p => p.users)
                .HasForeignKey(d => d.role_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("users_role_id_fkey");
        });

        modelBuilder.Entity<vending_machine>(entity =>
        {
            entity.HasKey(e => e.id).HasName("vending_machines_pkey");

            entity.ToTable("vending_machines", "automata");

            entity.HasIndex(e => e.company_id, "ix_vending_machines_company_id");

            entity.HasIndex(e => e.name, "ix_vending_machines_name");

            entity.HasIndex(e => e.status_id, "ix_vending_machines_status_id");

            entity.HasIndex(e => e.modem_id, "uq_vending_machine_modem_id_not_null")
                .IsUnique()
                .HasFilter("(modem_id IS NOT NULL)");

            entity.HasIndex(e => e.machine_number, "vending_machines_machine_number_key").IsUnique();

            entity.Property(e => e.id).HasDefaultValueSql("nextval('vending_machines_id_seq'::regclass)");
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.kit_online_cashbox_id).HasMaxLength(100);
            entity.Property(e => e.latitude).HasPrecision(9, 6);
            entity.Property(e => e.longitude).HasPrecision(9, 6);
            entity.Property(e => e.machine_number).HasMaxLength(100);
            entity.Property(e => e.name).HasMaxLength(255);
            entity.Property(e => e.total_income).HasPrecision(14, 2);
            entity.Property(e => e.updated_at).HasDefaultValueSql("now()");
            entity.Property(e => e.work_mode).HasMaxLength(100);
            entity.Property(e => e.work_time_text).HasMaxLength(32);

            entity.HasOne(d => d.client_company).WithMany(p => p.vending_machineclient_companies)
                .HasForeignKey(d => d.client_company_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_client_company_id_fkey");

            entity.HasOne(d => d.company).WithMany(p => p.vending_machinecompanies)
                .HasForeignKey(d => d.company_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_company_id_fkey");

            entity.HasOne(d => d.critical_value_template).WithMany(p => p.vending_machines)
                .HasForeignKey(d => d.critical_value_template_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_critical_value_template_id_fkey");

            entity.HasOne(d => d.engineer_user).WithMany(p => p.vending_machineengineer_users)
                .HasForeignKey(d => d.engineer_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("vending_machines_engineer_user_id_fkey");

            entity.HasOne(d => d.manager_user).WithMany(p => p.vending_machinemanager_users)
                .HasForeignKey(d => d.manager_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("vending_machines_manager_user_id_fkey");

            entity.HasOne(d => d.manufacturer).WithMany(p => p.vending_machinemanufacturers)
                .HasForeignKey(d => d.manufacturer_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_manufacturer_id_fkey");

            entity.HasOne(d => d.model).WithMany(p => p.vending_machinemodels)
                .HasForeignKey(d => d.model_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_model_id_fkey");

            entity.HasOne(d => d.modem).WithOne(p => p.vending_machine)
                .HasForeignKey<vending_machine>(d => d.modem_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("vending_machines_modem_id_fkey");

            entity.HasOne(d => d.notification_template).WithMany(p => p.vending_machines)
                .HasForeignKey(d => d.notification_template_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_notification_template_id_fkey");

            entity.HasOne(d => d.product_matrix).WithMany(p => p.vending_machines)
                .HasForeignKey(d => d.product_matrix_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_product_matrix_id_fkey");

            entity.HasOne(d => d.rfid_collection_card).WithMany(p => p.vending_machinerfid_collection_cards)
                .HasForeignKey(d => d.rfid_collection_card_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("vending_machines_rfid_collection_card_id_fkey");

            entity.HasOne(d => d.rfid_loading_card).WithMany(p => p.vending_machinerfid_loading_cards)
                .HasForeignKey(d => d.rfid_loading_card_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("vending_machines_rfid_loading_card_id_fkey");

            entity.HasOne(d => d.rfid_service_card).WithMany(p => p.vending_machinerfid_service_cards)
                .HasForeignKey(d => d.rfid_service_card_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("vending_machines_rfid_service_card_id_fkey");

            entity.HasOne(d => d.service_priority).WithMany(p => p.vending_machines)
                .HasForeignKey(d => d.service_priority_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_service_priority_id_fkey");

            entity.HasOne(d => d.slave_manufacturer).WithMany(p => p.vending_machineslave_manufacturers)
                .HasForeignKey(d => d.slave_manufacturer_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_slave_manufacturer_id_fkey");

            entity.HasOne(d => d.slave_model).WithMany(p => p.vending_machineslave_models)
                .HasForeignKey(d => d.slave_model_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_slave_model_id_fkey");

            entity.HasOne(d => d.status).WithMany(p => p.vending_machines)
                .HasForeignKey(d => d.status_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_status_id_fkey");

            entity.HasOne(d => d.technician_user).WithMany(p => p.vending_machinetechnician_users)
                .HasForeignKey(d => d.technician_user_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("vending_machines_technician_user_id_fkey");

            entity.HasOne(d => d.timezone).WithMany(p => p.vending_machines)
                .HasForeignKey(d => d.timezone_id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vending_machines_timezone_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
