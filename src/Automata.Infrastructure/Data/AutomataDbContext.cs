using Automata.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Automata.Infrastructure.Data;

/// <summary>
/// EF Core контекст проекта Automata.
/// Содержит маппинг актуальной схемы БД и связи между сущностями.
/// </summary>
public class AutomataDbContext : DbContext
{
    public AutomataDbContext(DbContextOptions<AutomataDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<MachineStatus> MachineStatuses => Set<MachineStatus>();
    public DbSet<MachineModel> MachineModels => Set<MachineModel>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Modem> Modems => Set<Modem>();
    public DbSet<ProductMatrix> ProductMatrices => Set<ProductMatrix>();
    public DbSet<CriticalValueTemplate> CriticalValueTemplates => Set<CriticalValueTemplate>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<VendingMachine> VendingMachines => Set<VendingMachine>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("automata");

        // --- Справочники ---
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();
        });

        modelBuilder.Entity<MachineStatus>(entity =>
        {
            entity.ToTable("machine_statuses");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();
        });

        modelBuilder.Entity<MachineModel>(entity =>
        {
            entity.ToTable("machine_models");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.Brand)
                .HasColumnName("brand")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.ModelName)
                .HasColumnName("model_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => new { e.Brand, e.ModelName })
                .IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            entity.Property(e => e.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.MiddleName)
                .HasColumnName("middle_name")
                .HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(320)
                .IsRequired();
            entity.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(32);
            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.RoleId)
                .HasColumnName("role_id")
                .IsRequired();
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Role)
                .WithMany(e => e.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            entity.Property(e => e.ParentCompanyId)
                .HasColumnName("parent_company_id");
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.ContactPerson)
                .HasColumnName("contact_person");
            entity.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(32);
            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(320);
            entity.Property(e => e.Address)
                .HasColumnName("address");
            entity.Property(e => e.Notes)
                .HasColumnName("notes");
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.ParentCompany)
                .WithMany(e => e.ChildCompanies)
                .HasForeignKey(e => e.ParentCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Modem>(entity =>
        {
            entity.ToTable("modems");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.ModemNumber)
                .HasColumnName("modem_number")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.Description)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            entity.HasIndex(e => e.ModemNumber).IsUnique();
        });

        modelBuilder.Entity<ProductMatrix>(entity =>
        {
            entity.ToTable("product_matrices");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(120)
                .IsRequired();

            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<CriticalValueTemplate>(entity =>
        {
            entity.ToTable("critical_value_templates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(120)
                .IsRequired();

            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.ToTable("notification_templates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(120)
                .IsRequired();

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // --- Основная карточка автомата ---
        modelBuilder.Entity<VendingMachine>(entity =>
        {
            entity.ToTable("vending_machines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.Location)
                .HasColumnName("location")
                .IsRequired();
            entity.Property(e => e.MachineModelId)
                .HasColumnName("machine_model_id")
                .IsRequired();
            entity.Property(e => e.SlaveMachineModelId)
                .HasColumnName("slave_machine_model_id");
            entity.Property(e => e.StatusId)
                .HasColumnName("status_id")
                .IsRequired();
            entity.Property(e => e.InstalledAt)
                .HasColumnName("installed_at")
                .IsRequired();
            entity.Property(e => e.LastServiceAt)
                .HasColumnName("last_service_at");
            entity.Property(e => e.TotalIncome)
                .HasColumnName("total_income")
                .HasPrecision(14, 2)
                .IsRequired();
            entity.Property(e => e.Address)
                .HasColumnName("address")
                .IsRequired();
            entity.Property(e => e.Place)
                .HasColumnName("place")
                .IsRequired();
            entity.Property(e => e.Coordinates)
                .HasColumnName("coordinates");
            entity.Property(e => e.MachineNumber)
                .HasColumnName("machine_number")
                .IsRequired();
            entity.Property(e => e.OperatingMode)
                .HasColumnName("operating_mode")
                .IsRequired();
            entity.Property(e => e.WorkingHours)
                .HasColumnName("working_hours");
            entity.Property(e => e.TimeZone)
                .HasColumnName("time_zone")
                .IsRequired();
            entity.Property(e => e.Notes)
                .HasColumnName("notes");
            entity.Property(e => e.KitOnlineCashboxId)
                .HasColumnName("kit_online_cashbox_id");
            entity.Property(e => e.ServicePriority)
                .HasColumnName("service_priority")
                .IsRequired();
            entity.Property(e => e.SupportsCoinAcceptor)
                .HasColumnName("supports_coin_acceptor")
                .IsRequired();
            entity.Property(e => e.SupportsBillAcceptor)
                .HasColumnName("supports_bill_acceptor")
                .IsRequired();
            entity.Property(e => e.SupportsCashlessModule)
                .HasColumnName("supports_cashless_module")
                .IsRequired();
            entity.Property(e => e.SupportsQrPayments)
                .HasColumnName("supports_qr_payments")
                .IsRequired();
            entity.Property(e => e.ServiceRfidCards)
                .HasColumnName("service_rfid_cards");
            entity.Property(e => e.CollectionRfidCards)
                .HasColumnName("collection_rfid_cards");
            entity.Property(e => e.LoadingRfidCards)
                .HasColumnName("loading_rfid_cards");
            entity.Property(e => e.ManagerUserId)
                .HasColumnName("manager_user_id");
            entity.Property(e => e.EngineerUserId)
                .HasColumnName("engineer_user_id");
            entity.Property(e => e.TechnicianOperatorUserId)
                .HasColumnName("technician_operator_user_id");
            entity.Property(e => e.ClientName)
                .HasColumnName("client_name");
            entity.Property(e => e.ModemId)
                .HasColumnName("modem_id");
            entity.Property(e => e.ProductMatrixId)
                .HasColumnName("product_matrix_id");
            entity.Property(e => e.CriticalValueTemplateId)
                .HasColumnName("critical_value_template_id");
            entity.Property(e => e.NotificationTemplateId)
                .HasColumnName("notification_template_id");

            entity.HasIndex(e => e.MachineNumber).IsUnique();

            entity.HasOne(e => e.MachineModel)
                .WithMany(e => e.VendingMachines)
                .HasForeignKey(e => e.MachineModelId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SlaveMachineModel)
                .WithMany()
                .HasForeignKey(e => e.SlaveMachineModelId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Status)
                .WithMany(e => e.VendingMachines)
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ManagerUser)
                .WithMany()
                .HasForeignKey(e => e.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EngineerUser)
                .WithMany()
                .HasForeignKey(e => e.EngineerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TechnicianOperatorUser)
                .WithMany()
                .HasForeignKey(e => e.TechnicianOperatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Modem)
                .WithMany(e => e.VendingMachines)
                .HasForeignKey(e => e.ModemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ProductMatrix)
                .WithMany(e => e.VendingMachines)
                .HasForeignKey(e => e.ProductMatrixId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CriticalValueTemplate)
                .WithMany(e => e.VendingMachines)
                .HasForeignKey(e => e.CriticalValueTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.NotificationTemplate)
                .WithMany(e => e.VendingMachines)
                .HasForeignKey(e => e.NotificationTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Товары, продажи, обслуживание ---
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            entity.Property(e => e.MachineId)
                .HasColumnName("machine_id")
                .IsRequired();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(e => e.Description)
                .HasColumnName("description");
            entity.Property(e => e.Price)
                .HasColumnName("price")
                .HasPrecision(10, 2)
                .IsRequired();
            entity.Property(e => e.Quantity)
                .HasColumnName("quantity")
                .IsRequired();
            entity.Property(e => e.MinStock)
                .HasColumnName("min_stock")
                .IsRequired();
            entity.Property(e => e.AvgDailySales)
                .HasColumnName("avg_daily_sales")
                .HasPrecision(10, 2)
                .IsRequired();

            entity.HasOne(e => e.Machine)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.MachineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("sales");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            entity.Property(e => e.MachineId)
                .HasColumnName("machine_id")
                .IsRequired();
            entity.Property(e => e.ProductId)
                .HasColumnName("product_id")
                .IsRequired();
            entity.Property(e => e.Quantity)
                .HasColumnName("quantity")
                .IsRequired();
            entity.Property(e => e.SaleAmount)
                .HasColumnName("sale_amount")
                .HasPrecision(12, 2)
                .IsRequired();
            entity.Property(e => e.SaleDatetime)
                .HasColumnName("sale_datetime")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(e => e.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(50)
                .IsRequired();

            entity.HasOne(e => e.Machine)
                .WithMany(e => e.Sales)
                .HasForeignKey(e => e.MachineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany(e => e.Sales)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.ToTable("maintenance_records");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            entity.Property(e => e.MachineId)
                .HasColumnName("machine_id")
                .IsRequired();
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            entity.Property(e => e.ServiceDate)
                .HasColumnName("service_date")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(e => e.WorkDescription)
                .HasColumnName("work_description")
                .IsRequired();
            entity.Property(e => e.Issues)
                .HasColumnName("issues");

            entity.HasOne(e => e.Machine)
                .WithMany(e => e.MaintenanceRecords)
                .HasForeignKey(e => e.MachineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                .WithMany(e => e.MaintenanceRecords)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
