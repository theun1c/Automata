using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class rfid_card
{
    public long id { get; set; }

    public string card_type { get; set; } = null!;

    public string card_number { get; set; } = null!;

    public virtual ICollection<vending_machine> vending_machinerfid_collection_cards { get; set; } = new List<vending_machine>();

    public virtual ICollection<vending_machine> vending_machinerfid_loading_cards { get; set; } = new List<vending_machine>();

    public virtual ICollection<vending_machine> vending_machinerfid_service_cards { get; set; } = new List<vending_machine>();
}
