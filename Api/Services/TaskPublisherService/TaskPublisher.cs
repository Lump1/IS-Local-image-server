using Contracts.Messages;
using Contracts;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace IS.ImageService.Api.Services.TaskPublisherService
{
    public class TaskPublisher : ITaskPublisher
    {
        private readonly IConnection _connection;

        public TaskPublisher(IConnection connection)
        {
            _connection = connection;
        }

        public async Task<Guid> PublishToQueueAsync(RBQ_Queues rbq_enum, ReadOnlyMemory<byte> body, CancellationToken ct = default)
        {
            await using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            var queueName = GetQueueName(rbq_enum);

            var jobId = Guid.NewGuid();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                body: body,
                basicProperties: new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent,
                    ContentType = "application/json",
                    MessageId = jobId.ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                },
                cancellationToken: ct);

            return jobId;
        }

        private string GetQueueName(RBQ_Queues rbq_enum)
        {
            return rbq_enum.ToString();
        }

        //public async Task<bool> TryPublishQueueAsync(RBQ_Queues rbq_enum, ReadOnlyMemory<byte> body, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        await PublishToQueueAsync(rbq_enum, body, ct);
        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
    }
}
