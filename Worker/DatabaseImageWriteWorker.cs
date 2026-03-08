using Contracts;
using IS.SharedServices.Services.TaskReceiverService;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Worker;

public class DatabaseImageWriteWorker : BackgroundService
{
    private readonly ILogger<DatabaseImageWriteWorker> _logger;
    private readonly ITaskReceiver _taskReceiver;
    private readonly IConfiguration _configuration;
    private readonly IConnection? _brokerConnection;
    private IChannel? _brokerChannel;

    private static readonly RBQ_Queues QueueName = RBQ_Queues.ProcessImage;

    public DatabaseImageWriteWorker(ILogger<DatabaseImageWriteWorker> logger, IConfiguration configuration, IConnection connection, ITaskReceiver taskReceiver)
    {
        _logger = logger;
        _configuration = configuration;
        _brokerConnection = connection;
        _taskReceiver = taskReceiver;
    }

    protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
    {
        await _taskReceiver.ReceiveAsync(
            QueueName,
            DateTime.UtcNow.AddMinutes(10),
            Expression: async (sender, args) => await ProcessMessageAsync(sender, args),
            ct: stoppingToken
        );
    
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
