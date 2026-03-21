namespace Automata.Application.Dashboard.Models;

public sealed class RecentSaleItem
{
    public DateTimeOffset SaleDate { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal SaleAmount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
}
