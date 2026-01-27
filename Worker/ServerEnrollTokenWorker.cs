using Contracts;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Worker;

public class ServerEnrollTokenWorker : BackgroundService
{
    private readonly ILogger<DatabaseImageWriteWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConnection? _brokerConnection;
    private readonly IDistributedCache _distributedCache;

    //private IChannel[]? _brokerChannels = new IChannel[3];
    //private RBQ_Queues[] RBQBrokerQueues = new RBQ_Queues[3] { RBQ_Queues.AuthKeyValidation };

    private IChannel? _brokerChannel;
    private string QueueName = RBQ_Queues.AuthKeyValidation.ToString();

    public ServerEnrollTokenWorker(
        ILogger<DatabaseImageWriteWorker> logger,
        IConfiguration configuration,
        IConnection? brokerConnection,
        IDistributedCache distributedCache)
    {
        _logger = logger;
        _configuration = configuration;
        _brokerConnection = brokerConnection;
        _distributedCache = distributedCache;
    }
    protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
    {
        _brokerChannel = await _brokerConnection!.CreateChannelAsync();
        await _brokerChannel.QueueDeclareAsync(RBQ_Queues.AuthKeyValidation.ToString(), exclusive: false);

        var consumer = new AsyncEventingBasicConsumer(_brokerChannel);
        consumer.ReceivedAsync += KeyValidationAsync;

        await _brokerChannel.BasicConsumeAsync(queue: QueueName, autoAck: true, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task KeyValidationAsync(object sender, BasicDeliverEventArgs args)
    {
        _logger.LogInformation($"Processed hardware key validation at {DateTime.UtcNow} with messageId: {args.BasicProperties.MessageId}");

        var messageByte = args.Body;
        var messageJson = await JsonSerializer.DeserializeAsync<Contracts.Messages.HardwareKeyValidation>(
            new MemoryStream(messageByte.ToArray()),
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

        var storedKeyBytes = await _distributedCache.GetAsync("ServerEnrollToken");

        if (storedKeyBytes is null)
        {
            _logger.LogWarning("No key found in cache. Please open system for registration.");
            return;
        }

        var storedKeyHash = System.Text.Encoding.ASCII.GetString(storedKeyBytes);
        if (messageJson is not null && messageJson.HardwareKey is not null && messageJson.HardwareKey.GetHashCode().ToString() == storedKeyHash)
        {
            _logger.LogInformation($"Hardware key for {messageJson.UserId} validation successful.");
        }
        else
        {
            _logger.LogWarning("Hardware key validation failed. Generating new key.");
            await SetNewKeyAsync();
        }
    }


    private async Task SetNewKeyAsync()
    {
        await _distributedCache.SetAsync("ServerEnrollToken", System.Text.Encoding.ASCII.GetBytes(KeyGeneration().GetHashCode().ToString()));
    }
    private Guid KeyGeneration(string format = "ddd-ddd-ddd")
    {
        var key = new Guid(format);
        return key;
    }
    //private string KeyGenerate()
    //{
        
    //}

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_brokerChannel is not null) await _brokerChannel.CloseAsync(cancellationToken);
            _brokerConnection?.CloseAsync();
        }
        finally
        {
            _brokerChannel?.Dispose();
            _brokerConnection?.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}

