using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using PowerFxLib.Models;
using System.Buffers;
using System.Text;
using UnifiedNamespace2025Lib.Services;
using YamlDotNet.RepresentationModel;

namespace UnifiedNamespace2025App.Services;

public class VirtualSignalsService(IConfiguration config, IServiceProvider sp): BackgroundService
{
    private PowerFxValue virtualSignalDefs;
    private RecalcEngine powerFxEngine;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var yamlStream = new YamlStream();
        var yeamlReader = new StreamReader(config[$"services:{"virtualSignals"}:configFileName"]);
        yamlStream.Load(yeamlReader);
        this.virtualSignalDefs = PowerFxValue.From(yamlStream.First().RootNode);

        var onTimerToEvaluateVirtualSignals = sp.GetKeyedService<MqttClient>(config[$"services:{"VirtualSignals"}:timerService"]);

        // POWERFX

        var powerFxConfig = new PowerFxConfig(Features.PowerFxV1);
        this.powerFxEngine = new RecalcEngine(powerFxConfig);

        onTimerToEvaluateVirtualSignals.Handle(async msg =>
        {
            if (msg.Topic.StartsWith("timers/"))
            {
                foreach (var (key, value) in virtualSignalDefs.SetValue)
                {
                    if (value.ValueType == PowerFxValueType.Formula)
                    {
                        var virtualSignalStatusTopic = $"virtualSignals/{key}/status";
                        try
                        {
                            var result = await powerFxEngine.EvalAsync(
                                value.FormulaValue,
                                CancellationToken.None
                            );
                            if (result.Type == FormulaType.Decimal)
                            {
                                await onTimerToEvaluateVirtualSignals.PublishAsync(key, result.AsDecimal());
                                await onTimerToEvaluateVirtualSignals.PublishAsync(virtualSignalStatusTopic, "ok");
                            }
                            else if (result.Type == FormulaType.String)
                            {
                                await onTimerToEvaluateVirtualSignals.PublishAsync(key, result.ToObject());
                                await onTimerToEvaluateVirtualSignals.PublishAsync(virtualSignalStatusTopic, "ok");
                            }
                            else
                            {
                                await onTimerToEvaluateVirtualSignals.PublishAsync(virtualSignalStatusTopic, "data type unknown");
                            }
                        }
                        catch (Exception ex)
                        {
                            await onTimerToEvaluateVirtualSignals.PublishAsync(virtualSignalStatusTopic, ex.Message);
                        }
                    }
                    else
                    {
                        await onTimerToEvaluateVirtualSignals.PublishAsync(key, value);
                    }
                }
            }
            else // cache filled
            {
                var json = Encoding.UTF8.GetString(msg.Payload.ToArray());
                // collect
                var value = FormulaValueJSON.FromJson(json);
                powerFxEngine.UpdateVariable(msg.Topic, value);
            }
        });
        await onTimerToEvaluateVirtualSignals.ConnectAsync();
    }
}