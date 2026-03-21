namespace Automata.Application.Machines.Models;

public sealed class MachineModelLookupItem
{
    public int Id { get; init; }
    public string Brand { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;

    public string DisplayName => $"{Brand} {ModelName}".Trim();
}
