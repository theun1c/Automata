namespace Automata.Application.Machines.Models;

public sealed class VendingMachineListItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string ModelDisplayName { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public DateOnly InstalledAt { get; init; }
    public DateOnly? LastServiceAt { get; init; }
    public decimal TotalIncome { get; init; }
}
