using Automata.Application.Common;

namespace Automata.Application.Machines.Models;

public sealed class VendingMachineEditorLookups
{
    public IReadOnlyList<MachineModelLookupItem> MachineModels { get; init; } = Array.Empty<MachineModelLookupItem>();
    public IReadOnlyList<LookupItem> Modems { get; init; } = Array.Empty<LookupItem>();
    public IReadOnlyList<LookupItem> ProductMatrices { get; init; } = Array.Empty<LookupItem>();
    public IReadOnlyList<LookupItem> CriticalValueTemplates { get; init; } = Array.Empty<LookupItem>();
    public IReadOnlyList<LookupItem> NotificationTemplates { get; init; } = Array.Empty<LookupItem>();
    public IReadOnlyList<UserLookupItem> Users { get; init; } = Array.Empty<UserLookupItem>();
}
