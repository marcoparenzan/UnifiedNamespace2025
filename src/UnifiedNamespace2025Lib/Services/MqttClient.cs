using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Packets;
using System.Text.Json;

namespace UnifiedNamespace2025Lib.Services;

public class MqttClient([ServiceKey] string serviceKey, IConfiguration conf, ILogger<MqttClient> logger)
{
    private MqttClientFactory mqttFactory;
    private IMqttClient mqttClient;

    private MqttClientSubscribeResult subResult;
    private MqttClientConnectResult connectResponse;

    public async Task ConnectAsync()
    {
        this.mqttFactory = new MqttClientFactory();
        this.mqttClient = mqttFactory.CreateMqttClient();

        var topicsString = conf[$"{serviceKey}:topics"] ?? "";
        var topics = topicsString switch {
            "" => [],
            _ => topicsString.Split(';').ToArray()
        };

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(conf[$"{serviceKey}:host"], int.Parse(conf[$"{serviceKey}:port"]))
            .WithClientId(conf[$"{serviceKey}:clientId"])
            .WithCredentials(conf[$"{serviceKey}:username"] ?? conf[$"{serviceKey}:clientId"], conf[$"{serviceKey}:password"])
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();

        // This will throw an exception if the server is not available.
        // The result from this message returns additional data which was sent
        // from the server. Please refer to the MQTT protocol specification for details.
        this.connectResponse = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        if (handlers.Count > 0)
        {
            mqttClient.ApplicationMessageReceivedAsync += (s) =>
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        handler(s.ApplicationMessage);
                    }
                    catch (Exception ex)
                    { 
                    
                    }
                }
                return Task.CompletedTask;
            };
        }

        if (topics.Length > 0)
        {
            var subOpts = new MqttClientSubscribeOptions
            {
                TopicFilters = topics.Select(xx => new MqttTopicFilter
                {
                    Topic = xx,
                    RetainHandling = MQTTnet.Protocol.MqttRetainHandling.SendAtSubscribe
                }).ToList()
            };
            this.subResult = await mqttClient.SubscribeAsync(subOpts);
        }
    }

    async Task DisconnectAsync()
    {
        // Send a clean disconnect to the server by calling _DisconnectAsync_. Without this the TCP connection
        // gets dropped and the server will handle this as a non clean disconnect (see MQTT spec for details).
        var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();

        await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
    }

    public async Task PublishAsync<TPayload>(string topic, TPayload payload, bool retainFlag = true)
    {
        if (mqttClient is null)
        {
            await ConnectAsync();
        }
        var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(JsonSerializer.Serialize(payload))
                    .WithRetainFlag(retainFlag)
                    .Build();

        var result = await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
    }

    List<Action<MqttApplicationMessage>> handlers = new();

    public void Handle(Action<MqttApplicationMessage> handler)
    {
        handlers.Add(handler);
    }
}
