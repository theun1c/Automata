namespace Automata.Application.Monitoring.Models;

public sealed class MonitoringFilterModel
{
    public string? Search { get; init; }
    public int? StatusId { get; init; }
    public string? SortBy { get; init; }
}
