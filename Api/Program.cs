using IS.DbCommon;
using IS.ImageService.Api.Services.FilterService;
using System.Text.Json.Serialization;
using IS.DbCommon.Models.DTO;
using Microsoft.EntityFrameworkCore;
using IS.ImageService.Api.Services.DeterminationService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using IS.SharedServices;
using IS.SharedServices.Services.CacheService;
using IS.SharedServices.Services.TaskReceiverService;


namespace Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //builder.Services.AddSingleton<IConnection>(sp =>
        //{
        //    var config = sp.GetRequiredService<IConfiguration>();

        //    var cs = config.GetConnectionString("broker")
        //     ?? throw new InvalidOperationException(
        //         "RabbitMQ connection string 'broker' is not configured.");

        //    var factory = new ConnectionFactory
        //    {
        //        Uri = new Uri(cs)
        //    };

        //    return factory.CreateConnectionAsync("images-api")
        //        .GetAwaiter()
        //        .GetResult();
        //});

        builder.AddRabbitMQClient(connectionName: "broker");

        builder.Services.AddSingleton<IS.SharedServices.Services.TaskPublisherService.ITaskPublisher, IS.SharedServices.Services.TaskPublisherService.TaskPublisher>();
        builder.Services.AddSingleton<IS.SharedServices.Services.CacheService.ICacher, IS.SharedServices.Services.CacheService.Cacher>();
        builder.Services.AddSingleton<IFileDeterminator, FileDeterminator>();
        builder.Services.AddSingleton<ITaskReceiver, TaskReceiver>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(jwtOptions =>
        {
            jwtOptions.Authority = "https://{--your-authority--}";
            jwtOptions.Audience = "https://{--your-audience--}";
        });


        builder.Services.AddScoped<IFilterImages, FilterImages>();

        builder.Services.AddDbContext<ImageServerEFContext>(op =>
        {
            var cs = builder.Configuration.GetConnectionString("imagesdb");
            op.UseNpgsql(cs);
        });

        builder.AddRedisDistributedCache(connectionName: "rediscache");

        builder.Services.AddControllers();

        builder.Logging.AddConsole();

        var app = builder.Build();

        // Dev Build

        if(app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ImageServerEFContext>();

            db.Database.Migrate();
        }

        //

        app.MapControllers();


        app.Run();
    }
}

[JsonSourceGenerationOptions(
PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
WriteIndented = false)]
[JsonSerializable(typeof(ImageSetDto))]
[JsonSerializable(typeof(List<ImageSetDto>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
