using Contracts;
using IS.SharedServices.Services.TaskReceiverService;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace Worker;

public class ServerEnrollTokenWorker : BackgroundService
{
    private readonly ILogger<DatabaseImageWriteWorker> _logger;
    private readonly ITaskReceiver _taskReceiver;
    private readonly IConfiguration _configuration;
    private readonly IConnection? _brokerConnection;
    private readonly IDistributedCache _distributedCache;

    //private IChannel[]? _brokerChannels = new IChannel[3];
    //private RBQ_Queues[] RBQBrokerQueues = new RBQ_Queues[3] { RBQ_Queues.AuthKeyValidation };

    private IChannel[]? channels = new IChannel[2];
    private RBQ_Queues[] QueueNames = new RBQ_Queues[2] { RBQ_Queues.AuthKeyValidation, RBQ_Queues.AuthKeyOpenTunnel };

    public ServerEnrollTokenWorker(
        ILogger<DatabaseImageWriteWorker> logger,
        IConfiguration configuration,
        IConnection? brokerConnection,
        IDistributedCache distributedCache, 
        ITaskReceiver taskReceiver)
    {
        _logger = logger;
        _configuration = configuration;
        _brokerConnection = brokerConnection;
        _distributedCache = distributedCache;
        _taskReceiver = taskReceiver;
    }
    protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
    {
        for (int i = 0; i < QueueNames.Length; i++)
        {
            Func<object?, BasicDeliverEventArgs, Task> expression = QueueNames[i] switch
            {
                RBQ_Queues.AuthKeyValidation => KeyValidationAsync,
                RBQ_Queues.AuthKeyOpenTunnel => SetNewKeyAsync,
                _ => throw new InvalidOperationException("Invalid queue name")
            };

            await _taskReceiver.ReceiveAsync(QueueNames[i], DateTime.UtcNow.AddMinutes(10), Expression: (sender, args) => expression(sender, args));
        }

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
            _logger.LogInformation($"Hardware key for job with ID: {args.BasicProperties.MessageId} validation successful.");

            await _distributedCache.SetAsync(messageJson.RedisAwaiterKey + ":" + args.BasicProperties.MessageId, new byte[] { 1 }, 
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });

            _logger.LogInformation($"Job with ID: {args.BasicProperties.MessageId}; result was published to cache with {messageJson.RedisAwaiterKey + ":" + args.BasicProperties.MessageId} key.");
            return;
        }
        else
        {
            _logger.LogWarning("Hardware key validation failed. Generating new key.");
            await SetNewKeyAsync(sender, args);
        }
    }


    private async Task SetNewKeyAsync(object sender, BasicDeliverEventArgs args)
    {
        _logger.LogInformation($"Processed set new hardware key at {DateTime.UtcNow} with messageId: {args.BasicProperties.MessageId}");

        var key = KeyGeneration();
        _logger.LogInformation($"Generated new hardware key: {key}");

        await _distributedCache.SetAsync("ServerEnrollToken", System.Text.Encoding.ASCII.GetBytes(key.GetHashCode().ToString()));
    }
    private string KeyGeneration(string format = "ddd-ddd-ddd")
    {
        var keyBuilder = new System.Text.StringBuilder();
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] == 'd')
            {
                keyBuilder.Append(RandomIntFromRNG(0, 10, rng));
            }
            else
            {
                keyBuilder.Append(format[i]);
            }
        }

        return keyBuilder.ToString();
    }

    int RandomIntFromRNG(int min, int max, RandomNumberGenerator CprytoRNG)
    {
        // Generate four random bytes
        byte[] four_bytes = new byte[4];
        CprytoRNG.GetBytes(four_bytes);

        // Convert the bytes to a UInt32
        UInt32 scale = BitConverter.ToUInt32(four_bytes, 0);

        // And use that to pick a random number >= min and < max
        return (int)(min + (max - min) * (scale / (uint.MaxValue + 1.0)));
    }

    //private Task<bool> SetQueuesAsync()
    //{
    //    throw new NotImplementedException();
    //}
    //private string KeyGenerate()
    //{

    //}

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < QueueNames.Length; i++)
        {
            try
            {
                if (channels[i] is not null) await channels[i]!.CloseAsync(cancellationToken);
                _brokerConnection?.CloseAsync();
            }
            finally
            {
                channels[i]?.Dispose();
                _brokerConnection?.Dispose();
            }
        }

        await base.StopAsync(cancellationToken);
    }
}

