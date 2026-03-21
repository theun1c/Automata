namespace Automata.Application.Monitoring.Models;

public sealed class MonitoringDashboardModel
{
    public IReadOnlyList<MonitoringMachineItem> Machines { get; init; } = [];
    public MonitoringSummaryModel Summary { get; init; } = new();
}
