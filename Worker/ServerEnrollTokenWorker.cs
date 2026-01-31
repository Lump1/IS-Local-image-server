using Contracts;
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
    private readonly IConfiguration _configuration;
    private readonly IConnection? _brokerConnection;
    private readonly IDistributedCache _distributedCache;

    //private IChannel[]? _brokerChannels = new IChannel[3];
    //private RBQ_Queues[] RBQBrokerQueues = new RBQ_Queues[3] { RBQ_Queues.AuthKeyValidation };

    private IChannel[]? channels = new IChannel[2];
    private string[] QueueNames = new string[2] { RBQ_Queues.AuthKeyValidation.ToString(), RBQ_Queues.AuthKeyOpenTunnel.ToString() };

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
        for (int i = 0; i < QueueNames.Length; i++)
        {
            channels[i] = await _brokerConnection!.CreateChannelAsync();
            await channels[i]!.QueueDeclareAsync(QueueNames[i], exclusive: false);
            var consumer = new AsyncEventingBasicConsumer(channels[i]);
            if (QueueNames[i] == RBQ_Queues.AuthKeyValidation.ToString())
            {
                consumer.ReceivedAsync += KeyValidationAsync;
            }
            else if (QueueNames[i] == RBQ_Queues.AuthKeyOpenTunnel.ToString())
            {
                consumer.ReceivedAsync += SetNewKeyAsync;
            }
            await channels[i]!.BasicConsumeAsync(queue: QueueNames[i], autoAck: true, consumer: consumer);
        }

        //_brokerChannel = await _brokerConnection!.CreateChannelAsync();
        //await _brokerChannel.QueueDeclareAsync(RBQ_Queues.AuthKeyValidation.ToString(), exclusive: false);

        //var consumer = new AsyncEventingBasicConsumer(_brokerChannel);
        //consumer.ReceivedAsync += KeyValidationAsync;

        //await _brokerChannel.BasicConsumeAsync(queue: QueueName, autoAck: true, consumer: consumer);

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

            //_distributedCache.Set(args.BasicProperties.MessageId)
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

