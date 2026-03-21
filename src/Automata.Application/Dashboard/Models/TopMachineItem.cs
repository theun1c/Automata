namespace Automata.Application.Dashboard.Models;

public sealed class TopMachineItem
{
    public string MachineName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public decimal TotalIncome { get; init; }
}
