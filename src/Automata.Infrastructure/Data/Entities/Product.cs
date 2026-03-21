namespace Automata.Infrastructure.Data.Entities;

public class Product
{
    public Guid Id { get; set; }
    public Guid MachineId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; }
    public decimal AvgDailySales { get; set; }

    public VendingMachine Machine { get; set; } = null!;
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
