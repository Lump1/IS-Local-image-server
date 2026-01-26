using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Worker;

public class DatabaseImageWriteWorker : BackgroundService
{
    private readonly ILogger<DatabaseImageWriteWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConnection? _brokerConnection;
    private IChannel? _brokerChannel;

    private static readonly string QueueName = RBQ_Queues.ProcessImage.ToString();

    public DatabaseImageWriteWorker(ILogger<DatabaseImageWriteWorker> logger, IConfiguration configuration, IConnection connection)
    {
        _logger = logger;
        _configuration = configuration;
        _brokerConnection = connection;
    }

    protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
    {
        _brokerChannel =  await _brokerConnection!.CreateChannelAsync();
        await _brokerChannel.QueueDeclareAsync(QueueName, exclusive: false);

        var consumer = new AsyncEventingBasicConsumer(_brokerChannel);
        consumer.ReceivedAsync += ProcessMessageAsync;

        await _brokerChannel.BasicConsumeAsync(queue: QueueName, autoAck: true, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task ProcessMessageAsync(object? sender, BasicDeliverEventArgs args)
    {
        _logger.LogInformation($"Processed message at {DateTime.UtcNow} with messageId: {args.BasicProperties.MessageId}");

        var messageByte = args.Body;
        var messageJson = await JsonSerializer.DeserializeAsync<Contracts.Messages.ProcessImage>(
            new MemoryStream(messageByte.ToArray()),
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

        // Handle the deserialized messageJson object as needed
    }

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
