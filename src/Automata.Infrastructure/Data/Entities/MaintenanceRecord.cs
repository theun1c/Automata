namespace Automata.Infrastructure.Data.Entities;

public class MaintenanceRecord
{
    public Guid Id { get; set; }
    public Guid MachineId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ServiceDate { get; set; }
    public string WorkDescription { get; set; } = null!;
    public string? Issues { get; set; }

    public VendingMachine Machine { get; set; } = null!;
    public User User { get; set; } = null!;
}
