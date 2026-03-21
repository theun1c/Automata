namespace Automata.Application.Dashboard.Models;

public sealed class DashboardSalesDynamicsItem
{
    public DateOnly Day { get; init; }
    public decimal Amount { get; init; }
    public int Quantity { get; init; }
}
