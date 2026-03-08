using Contracts;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IS.SharedServices.Services.TaskReceiverService
{
    public interface ITaskReceiver
    {
        Task ReceiveAsync(
            RBQ_Queues rbq_queue,
            DateTime tunnelExpiring,
            AsyncEventHandler<BasicDeliverEventArgs> handler,
            CancellationToken ct = default);
        
        Task ReceiveAsync(
            RBQ_Queues rbq_queue,
            DateTime tunnelExpiring,
            Func<object?, BasicDeliverEventArgs, Task> Expression,
            CancellationToken ct = default);
    }
}
