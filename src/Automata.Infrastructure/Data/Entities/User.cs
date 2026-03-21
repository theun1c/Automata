namespace Automata.Infrastructure.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public string LastName { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = null!;
    public int RoleId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Role Role { get; set; } = null!;
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}
