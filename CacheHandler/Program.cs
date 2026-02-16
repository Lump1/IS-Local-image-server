namespace CacheHandler;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddRabbitMQClient(connectionName: "broker");
        builder.AddRedisDistributedCache(connectionName: "rediscache");

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}