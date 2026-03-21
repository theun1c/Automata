namespace Automata.Application.Monitoring.Models;

public sealed class MonitoringSummaryModel
{
    public int TotalMachines { get; init; }
    public int WorkingMachines { get; init; }
    public int NotWorkingMachines { get; init; }
    public int AttentionRequiredMachines { get; init; }
    public int TotalProducts { get; init; }
    public int LowStockProducts { get; init; }
    public decimal TotalIncome { get; init; }
}
