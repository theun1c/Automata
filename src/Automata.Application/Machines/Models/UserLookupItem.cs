namespace Automata.Application.Machines.Models;

public sealed class UserLookupItem
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public int RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
}
