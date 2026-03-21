namespace Automata.Application.Inventory.Models;

/// <summary>
/// DTO формы создания/редактирования товара в учете ТМЦ.
/// </summary>
public sealed class ProductEditModel
{
    public Guid? Id { get; init; }
    public Guid MachineId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public int MinStock { get; init; }
    public decimal AvgDailySales { get; init; }
}
