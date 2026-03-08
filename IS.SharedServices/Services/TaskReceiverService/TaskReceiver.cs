using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace IS.SharedServices.Services.TaskReceiverService
{
    public class TaskReceiver : ITaskReceiver
    {
        private readonly IConnection _connection;

        public TaskReceiver(IConnection connection)
        {
            _connection = connection;
        }

        public async Task ReceiveAsync(RBQ_Queues rbq_queue, DateTime tunnelExpiring, AsyncEventHandler<BasicDeliverEventArgs> handler, CancellationToken ct = default) 
        { 
            var channel = await OpenTunnelAsync(rbq_queue.ToString(), tunnelExpiring, ct);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += handler;

            await channel!.BasicConsumeAsync(queue: rbq_queue.ToString(), autoAck: true, consumer: consumer);
        }
        public async Task ReceiveAsync(RBQ_Queues rbq_queue, DateTime tunnelExpiring, Func<object?, BasicDeliverEventArgs, Task> Expression, CancellationToken ct = default)
        {
            var channel = await OpenTunnelAsync(rbq_queue.ToString(), tunnelExpiring, ct);
            var consumer = CreateConsumerWithExpression(channel, Expression);

            await channel!.BasicConsumeAsync(queue: rbq_queue.ToString(), autoAck: true, consumer: consumer);
        }

        private async Task<IChannel> OpenTunnelAsync(string queueName, DateTime tunnelExpiring, CancellationToken ct = default)
        {
            var channel = await _connection!.CreateChannelAsync();
            await channel!.QueueDeclareAsync(queueName, exclusive: false, cancellationToken: ct);

            return channel;
        }
        private AsyncEventingBasicConsumer CreateConsumerWithHandler(IChannel channel, AsyncEventHandler<BasicDeliverEventArgs> handler)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += handler;
            
            return consumer;
        }

        private AsyncEventingBasicConsumer CreateConsumerWithExpression(IChannel channel, Func<object?, BasicDeliverEventArgs, Task> Expression)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (sender, ea) => Expression(sender, ea);

            return consumer;
        }
    }
}
