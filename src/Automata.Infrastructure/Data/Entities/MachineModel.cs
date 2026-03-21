namespace Automata.Infrastructure.Data.Entities;

public class MachineModel
{
    public int Id { get; set; }
    public string Brand { get; set; } = null!;
    public string ModelName { get; set; } = null!;

    public ICollection<VendingMachine> VendingMachines { get; set; } = new List<VendingMachine>();
}
