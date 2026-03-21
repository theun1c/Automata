namespace Automata.Infrastructure.Data.Entities;

public class Sale
{
    public Guid Id { get; set; }
    public Guid MachineId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal SaleAmount { get; set; }
    public DateTimeOffset SaleDatetime { get; set; }
    public string PaymentMethod { get; set; } = null!;

    public VendingMachine Machine { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
