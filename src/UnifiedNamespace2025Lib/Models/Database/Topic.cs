using System;
using System.Collections.Generic;

namespace UnifiedNamespace2025Lib.Models.Database;

public partial class Topic
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public virtual ICollection<TopicValue> TopicValues { get; set; } = new List<TopicValue>();
}
