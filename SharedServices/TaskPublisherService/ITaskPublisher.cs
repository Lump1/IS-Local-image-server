using Contracts;

namespace IS.ImageService.Api.Services.TaskPublisherService
{
    public interface ITaskPublisher
    {
        Task<Guid> PublishToQueueAsync(RBQ_Queues rbqEnum, ReadOnlyMemory<byte> body, CancellationToken ct = default);
    }
}
