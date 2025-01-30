using System;
using System.Collections.Generic;

namespace UnifiedNamespace2025Lib.Models.Database;

public partial class ValueType
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<TopicValue> TopicValues { get; set; } = new List<TopicValue>();
}
