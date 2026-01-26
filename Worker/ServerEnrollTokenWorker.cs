using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Worker;

namespace IS.ImageService.Worker;

public class ServerEnrollTokenWorker : BackgroundService
{
    private readonly ILogger<DatabaseImageWriteWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConnection? _brokerConnection;

    //private IChannel[]? _brokerChannels = new IChannel[3];
    //private RBQ_Queues[] RBQBrokerQueues = new RBQ_Queues[3] { RBQ_Queues.AuthKeyValidation };

    private IChannel? _brokerChannel;
    private string QueueName = RBQ_Queues.AuthKeyValidation.ToString();

    public ServerEnrollTokenWorker(
        ILogger<DatabaseImageWriteWorker> logger,
        IConfiguration configuration,
        IConnection? brokerConnection)
    {
        _logger = logger;
        _configuration = configuration;
        _brokerConnection = brokerConnection;
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
        // Implement key validation logic here
    }
    private async Task KeyGenerationAsync()
    {
        // Implement key generate logic here
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

