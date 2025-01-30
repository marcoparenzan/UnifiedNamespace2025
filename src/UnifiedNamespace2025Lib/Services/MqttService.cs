using UnifiedNamespace2025Lib.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace UnifiedNamespace2025Lib.Services;

public class MqttService([FromKeyedServices("retain")] IRetainedMessage retain) : BackgroundService
{
    private MqttServer server;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttServerFactory = new MqttServerFactory();

        // Due to security reasons the "default" endpoint (which is unencrypted) is not enabled by default!
        var mqttServerOptions = mqttServerFactory
            .CreateServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(2883)
            .Build();

        this.server = mqttServerFactory.CreateMqttServer(mqttServerOptions);
        server.ClientConnectedAsync += async (o) =>
        {
            // retained messages
            Console.WriteLine($"{o.ClientId} connected");
        };

        server.ValidatingConnectionAsync += async e =>
        {
            //if (e.ClientId != "dt")
            //{
            //    e.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            //    return;
            //}

            if (e.UserName != "ValidUser" && e.UserName != e.ClientId)
            {
                e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }

            if (e.Password != "SecretPassword")
            {
                e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }

            e.ReasonCode = MqttConnectReasonCode.Success;
        };

        // Make sure that the server will load the retained messages.
        server.LoadingRetainedMessageAsync += async eventArgs =>
        {
            var messages = await retain.GetAsync();
            eventArgs.LoadedRetainedMessages = messages.Select(xx => xx.ToApplicationMessage()).ToList();
        };

        // Make sure to persist the changed retained messages.
        server.RetainedMessageChangedAsync += async eventArgs =>
        {
            await retain.AddAsync(MqttRetainedMessageModel.Create(eventArgs.ChangedRetainedMessage));
        };

        server.RetainedMessagesClearedAsync += async(a) =>
        {
        };

        await server.StartAsync();
    }
}
