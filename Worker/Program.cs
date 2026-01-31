using RabbitMQ.Client;

namespace Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddRabbitMQClient(connectionName: "broker");
        builder.AddRedisDistributedCache(connectionName: "rediscache");


        builder.Services.AddHostedService<DatabaseImageWriteWorker>();
        builder.Services.AddHostedService<ServerEnrollTokenWorker>();


        var host = builder.Build();
        host.Run();
    }
}