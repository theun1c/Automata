namespace Automata.Infrastructure.Data.Entities;

public class CriticalValueTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<VendingMachine> VendingMachines { get; set; } = new List<VendingMachine>();
}
