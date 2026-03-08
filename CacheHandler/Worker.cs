using Contracts;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json.Serialization;
using System.Text.Json;
using IS.SharedServices.Services.TaskPublisherService;
using IS.SharedServices.Services.TaskReceiverService;

namespace CacheHandler;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDistributedCache _distributedCache;
    private readonly IConnection? _brokerConnection;
    private readonly ITaskPublisher _taskPublisher;
    private readonly ITaskReceiver _taskReceiver;

    private IChannel? channel;
    private RBQ_Queues QueueName = RBQ_Queues.SetCacheHandlerListener;

    private int HopDelay = 5000;

    public Worker(ILogger<Worker> logger, IDistributedCache distributedCache, IConnection? brokerConnection, ITaskPublisher taskPublisher, ITaskReceiver taskReceiver)
    {
        _logger = logger;
        _distributedCache = distributedCache;
        _brokerConnection = brokerConnection;
        _taskPublisher = taskPublisher;
        _taskReceiver = taskReceiver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _taskReceiver.ReceiveAsync(
            QueueName, 
            DateTime.UtcNow.AddMinutes(10), 
            Expression: async (sender, args) => await SetListener(sender, args)
         );
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
            await _taskPublisher.PublishToQueueAsync(RBQ_Queues.RollbackMessage, messageJson.body, jobIdForRollback: messageJson.jobKey);
        }
        else
        {
            _logger.LogWarning($"Cache Handler Listener failed to get any information after {messageJson.hopCount} attempts: {DateTime.UtcNow}");
        }
    }
}
