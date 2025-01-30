using UnifiedNamespace2025Lib.Services;

namespace UnifiedNamespace2025App.Services;

public class FileSensorService([FromKeyedServices("fileSensor")] MqttClient fileSensor) : BackgroundService
{
    private Task<Task> task;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.task = Task.Factory.StartNew(async () =>
        {
            var fileName = @"Data\data_acme_2020_07_007955_0x40_a.csv";
            var items = 
                File.ReadAllLines(fileName)
                .Skip(1)
                .Select(xx => xx.Split(';'))
                .Select(xx => new { 
                    TimeStamp = DateTimeOffset.Parse(xx[5]),
                    Value = decimal.Parse(xx[6])
                })
                .ToArray()
            ;

            await fileSensor.ConnectAsync();

            var i = 0;
            var item = items[i];
            while (true)
            {
                try
                {
                    await PublishAsync("sensors/filesensor/temperature", new
                    {
                        Timestamp = DateTimeOffset.Now,
                        Value = item.Value
                    });
                }
                catch (Exception ex)
                { 
                }

                i++;
                if (i == items.Length)
                {
                    i = 0;
                }
                var item1 = items[i];
                var delay = (int)(item1.TimeStamp - item.TimeStamp).TotalMilliseconds / 60;
                await Task.Delay(delay);
                item = item1;
            }
        });
    }

    async Task PublishAsync<TTarget>(string topic, TTarget body)
    {
        await fileSensor.PublishAsync(topic, body, retainFlag:false);
    }
}
