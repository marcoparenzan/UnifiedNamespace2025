using UnifiedNamespace2025Lib.Models;

namespace UnifiedNamespace2025Lib.Services;
 
public interface IRetainedMessage
{
    Task AddAsync(MqttRetainedMessageModel value);
    Task<MqttRetainedMessageModel[]> GetAsync(params string[] keys);
}
