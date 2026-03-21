namespace Automata.Application.Inventory.Models;

public sealed class ProductListItem
{
    public Guid Id { get; init; }
    public Guid MachineId { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public int MinStock { get; init; }
    public decimal AvgDailySales { get; init; }
    public bool IsLowStock { get; init; }
}
