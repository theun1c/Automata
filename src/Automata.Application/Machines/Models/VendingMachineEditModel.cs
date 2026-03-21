namespace Automata.Application.Machines.Models;

/// <summary>
/// DTO формы создания/редактирования торгового автомата.
/// </summary>
public sealed class VendingMachineEditModel
{
    public Guid? Id { get; init; }

    // Основная информация об автомате.
    public string Name { get; init; } = string.Empty;
    public int MachineModelId { get; init; }
    public int? SlaveMachineModelId { get; init; }
    public int StatusId { get; init; } = 1;
    public DateOnly InstalledAt { get; init; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? LastServiceAt { get; init; }
    public decimal TotalIncome { get; init; }

    // Локация и идентификация.
    public string Address { get; init; } = string.Empty;
    public string Place { get; init; } = string.Empty;
    public string? Coordinates { get; init; }
    public string MachineNumber { get; init; } = string.Empty;

    // Параметры эксплуатации.
    public string OperatingMode { get; init; } = string.Empty;
    public string? WorkingHours { get; init; }
    public string TimeZone { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? KitOnlineCashboxId { get; init; }
    public string ServicePriority { get; init; } = string.Empty;

    // Платежные опции.
    public bool SupportsCoinAcceptor { get; init; }
    public bool SupportsBillAcceptor { get; init; }
    public bool SupportsCashlessModule { get; init; }
    public bool SupportsQrPayments { get; init; }

    // RFID-настройки.
    public string? ServiceRfidCards { get; init; }
    public string? CollectionRfidCards { get; init; }
    public string? LoadingRfidCards { get; init; }

    // Ответственные сотрудники и клиент.
    public Guid? ManagerUserId { get; init; }
    public Guid? EngineerUserId { get; init; }
    public Guid? TechnicianOperatorUserId { get; init; }
    public string? ClientName { get; init; }

    // Справочники editor-формы.
    public int? ModemId { get; init; }
    public int? ProductMatrixId { get; init; }
    public int? CriticalValueTemplateId { get; init; }
    public int? NotificationTemplateId { get; init; }
}
