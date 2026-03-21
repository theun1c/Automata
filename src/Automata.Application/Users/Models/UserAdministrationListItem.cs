namespace Automata.Application.Users.Models;

public sealed class UserAdministrationListItem
{
    public Guid Id { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public int RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public bool HasMaintenanceRecords { get; init; }
}
