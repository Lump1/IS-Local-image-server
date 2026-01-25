using RabbitMQ.Client;

namespace Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddRabbitMQClient(connectionName: "broker");
        builder.AddRedisClient(connectionName: "rediscache");


        builder.Services.AddHostedService<DatabaseImageWrite>();


        var host = builder.Build();
        host.Run();
    }
}