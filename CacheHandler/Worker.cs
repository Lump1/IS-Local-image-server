using Contracts;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace CacheHandler;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDistributedCache _distributedCache;
    private readonly IConnection? _brokerConnection;

    private IChannel? channel;
    private string QueueName = RBQ_Queues.SetCacheHandlerListener.ToString();

    private int HopDelay = 5000;

    public Worker(ILogger<Worker> logger, IDistributedCache distributedCache, IConnection? brokerConnection)
    {
        _logger = logger;
        _distributedCache = distributedCache;
        _brokerConnection = brokerConnection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        channel = await _brokerConnection!.CreateChannelAsync();
        await channel!.QueueDeclareAsync(QueueName, exclusive: false);
        var consumer = new AsyncEventingBasicConsumer(channel);
        if (QueueName == RBQ_Queues.SetCacheHandlerListener.ToString())
        {
            consumer.ReceivedAsync += SetListener;
        }
        await channel!.BasicConsumeAsync(queue: QueueName, autoAck: true, consumer: consumer);
    }

    private async Task SetListener(object sender, BasicDeliverEventArgs args)
    {
        var messageByte = args.Body;
        var messageJson = await JsonSerializer.DeserializeAsync<Contracts.Messages.CacheHandlerSetter>(
            new MemoryStream(messageByte.ToArray()),
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

        bool isReady = false;
        int hopCount = messageJson.hopCount;

        while (!isReady || hopCount > 0)
        {
            try
            {
                var result = await _distributedCache.GetAsync(messageJson.jobKey, CancellationToken.None);
                if (result is not null && result[0] == 1)
                {
                    isReady = true;
                    _logger.LogInformation($"Cache Handler Listener handled succesful result: {DateTime.UtcNow}");
                }
                else
                {
                    _logger.LogInformation($"Cache Handler Listener still listening: {DateTime.UtcNow}");
                    hopCount -= 1;
                    await Task.Delay(HopDelay);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to set Cache Handler Listener to ready: {ex.Message}");
                hopCount -= 1;
                await Task.Delay(HopDelay);
            }
        }

        if(isReady)
        {
            // Open channel for sending result back
        }
        else
        {
            _logger.LogWarning($"Cache Handler Listener failed to get any information after {messageJson.hopCount} attempts: {DateTime.UtcNow}");
        }
    }
}
