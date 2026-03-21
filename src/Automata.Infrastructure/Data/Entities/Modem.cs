namespace Automata.Infrastructure.Data.Entities;

public class Modem
{
    public int Id { get; set; }
    public string ModemNumber { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public ICollection<VendingMachine> VendingMachines { get; set; } = new List<VendingMachine>();
}
