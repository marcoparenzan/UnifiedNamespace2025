using UnifiedNamespace2025Lib.Services;

namespace UnifiedNamespace2025App.Services;

public class TimerServices([FromKeyedServices("timerGenerator")] MqttClient timerGenerator): BackgroundService
{
    private Task<Task> task;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.task = Task.Factory.StartNew(async () =>
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await periodicTimer.WaitForNextTickAsync())
            {
                var now = DateTimeOffset.Now;
                var body = new
                {
                    now = now
                };

                if (now.Second % 5 == 0)
                {
                    await PublishAsync($"timers/5s", body);
                    if (now.Second % 15 == 0)
                    {
                        await PublishAsync($"timers/15s", body);
                    }
                    if (now.Second == 0)
                    {
                        await PublishAsync($"timers/1m", body);
                        if (now.Minute == 0)
                        {
                            await PublishAsync($"timers/1h", body);
                        }
                    }
                }
            }
        });
    }

    async Task PublishAsync<TTarget>(string topic, TTarget body)
    {
        await timerGenerator.PublishAsync(topic, body, retainFlag:false);
    }
}
