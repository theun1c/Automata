namespace Automata.Application.Dashboard.Models;

public sealed class RecentMaintenanceItem
{
    public DateTimeOffset ServiceDate { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public string EngineerName { get; init; } = string.Empty;
    public string WorkDescription { get; init; } = string.Empty;
}
