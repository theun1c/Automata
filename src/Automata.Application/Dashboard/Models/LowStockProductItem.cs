namespace Automata.Application.Dashboard.Models;

public sealed class LowStockProductItem
{
    public string ProductName { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int MinStock { get; init; }
    public string StockStateText => $"{Quantity}/{MinStock}";
}
