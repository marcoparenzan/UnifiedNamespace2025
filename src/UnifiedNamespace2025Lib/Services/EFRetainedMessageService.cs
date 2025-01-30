
using UnifiedNamespace2025Lib.Models;
using UnifiedNamespace2025Lib.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text.Json;

namespace UnifiedNamespace2025Lib.Services;

public class EFRetainedMessageService : UnifiedNamespaceContext, IRetainedMessage
{
    private Models.Database.ValueType[] valueTypes;
    private Models.Database.ValueType jsonValueType;
    private HybridCache hybridCache;

    public EFRetainedMessageService(DbContextOptions<UnifiedNamespaceContext> options, HybridCache hybridCache) : base(options)
    {
        this.valueTypes = ValueTypes.ToArray();
        this.jsonValueType = valueTypes.Single(xx => xx.Id == 1);
        this.hybridCache = hybridCache;
    }

    public async Task AddAsync(MqttRetainedMessageModel value)
    {
        var ts = DateTimeOffset.Now;
        var topic = Topics.SingleOrDefault(xx => xx.Value == value.Topic);
        if (topic is null)
        {
            topic = new Topic
            {
                Value = value.Topic
            };
            Topics.Add(topic);
        }
        var lastTopicValue = TopicValues.SingleOrDefault(xx => xx.TopicId == topic.Id && xx.To == null);
        var newTopicValue = new TopicValue
        {
            Topic = topic,
            From = ts,
            ValueType = jsonValueType,
            Value = JsonSerializer.Serialize(value)
        };
        TopicValues.Add(newTopicValue);
        if (lastTopicValue is not null) lastTopicValue.To = ts;

        await hybridCache.SetAsync(value.Topic, value);

        await SaveChangesAsync();
    }

    public async Task<MqttRetainedMessageModel[]> GetAsync(params string[] keys)
    {
        var items = new List<MqttRetainedMessageModel>();

        if (keys.Length == 0)
        {
            keys = Topics.Select(xx => xx.Value).ToArray();
        }

        foreach (var key in keys)
        {
            var item = await hybridCache.GetOrCreateAsync(key, async ct =>
            {
                var xxx = TopicValues.SingleOrDefault(xx => xx.Topic.Value == key && xx.To == null);
                var itemXX = JsonSerializer.Deserialize<MqttRetainedMessageModel>(xxx.Value);
                return itemXX;
            });
            items.Add(item);
        }

        return items.ToArray();
    }
}
