using Contracts.Messages;
using Contracts;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace IS.SharedServices.Services.TaskPublisherService
{
    public class TaskPublisher : ITaskPublisher
    {
        private readonly IConnection _connection;

        public TaskPublisher(IConnection connection)
        {
            _connection = connection;
        }

        public async Task<Guid> PublishToQueueAsync(RBQ_Queues rbq_enum, ReadOnlyMemory<byte> body, CancellationToken ct = default, string? jobIdForRollback = null)
        {
            return await PublishToQueueAsync(jobIdForRollback == null ? GetQueueName(rbq_enum) : RollBackMessageBuilderStatic.RollBackMessageBuilder.RollbackMessageBuild(rbq_enum, jobIdForRollback), body, ct);
        }
        private async Task<Guid> PublishToQueueAsync(string rbq_message, ReadOnlyMemory<byte> body, CancellationToken ct = default)
        {
            await using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            var queueName = rbq_message;

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
