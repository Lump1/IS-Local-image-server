namespace CacheHandler;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddRabbitMQClient(connectionName: "broker");
        builder.AddRedisDistributedCache(connectionName: "rediscache");

        builder.Services.AddSingleton<
            IS.SharedServices.Services.TaskPublisherService.ITaskPublisher, 
            IS.SharedServices.Services.TaskPublisherService.TaskPublisher
        >();

        builder.Services.AddSingleton<
            IS.SharedServices.Services.CacheService.ICacher,
            IS.SharedServices.Services.CacheService.Cacher
        >();

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}