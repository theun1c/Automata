using System;
using System.Collections.Generic;

namespace Automata.Infrastructure.Data.Entities.Generated;

public partial class news_item
{
    public long id { get; set; }

    public string title { get; set; } = null!;

    public string body { get; set; } = null!;

    public DateTime published_at { get; set; }

    public bool is_active { get; set; }
}
