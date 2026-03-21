namespace Automata.Infrastructure.Data.Entities;

public class VendingMachine
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Location { get; set; } = null!;
    public int MachineModelId { get; set; }
    public int? SlaveMachineModelId { get; set; }
    public int StatusId { get; set; }
    public DateOnly InstalledAt { get; set; }
    public DateOnly? LastServiceAt { get; set; }
    public decimal TotalIncome { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string? Coordinates { get; set; }
    public string MachineNumber { get; set; } = string.Empty;
    public string OperatingMode { get; set; } = string.Empty;
    public string? WorkingHours { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? KitOnlineCashboxId { get; set; }
    public string ServicePriority { get; set; } = string.Empty;
    public bool SupportsCoinAcceptor { get; set; }
    public bool SupportsBillAcceptor { get; set; }
    public bool SupportsCashlessModule { get; set; }
    public bool SupportsQrPayments { get; set; }
    public string? ServiceRfidCards { get; set; }
    public string? CollectionRfidCards { get; set; }
    public string? LoadingRfidCards { get; set; }
    public Guid? ManagerUserId { get; set; }
    public Guid? EngineerUserId { get; set; }
    public Guid? TechnicianOperatorUserId { get; set; }
    public string? ClientName { get; set; }
    public int? ModemId { get; set; }
    public int? ProductMatrixId { get; set; }
    public int? CriticalValueTemplateId { get; set; }
    public int? NotificationTemplateId { get; set; }

    public MachineModel MachineModel { get; set; } = null!;
    public MachineModel? SlaveMachineModel { get; set; }
    public MachineStatus Status { get; set; } = null!;
    public User? ManagerUser { get; set; }
    public User? EngineerUser { get; set; }
    public User? TechnicianOperatorUser { get; set; }
    public Modem? Modem { get; set; }
    public ProductMatrix? ProductMatrix { get; set; }
    public CriticalValueTemplate? CriticalValueTemplate { get; set; }
    public NotificationTemplate? NotificationTemplate { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}
