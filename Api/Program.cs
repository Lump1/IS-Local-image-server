using Contracts;
using IS.DbCommon;
using IS.ImageService.Api.Services;
using IS.ImageService.Api.Services.FilterService;
using IS.ImageService.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using IS.DbCommon.Models;
using IS.DbCommon.Models.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.JSInterop.Infrastructure;
using SixLabors.ImageSharp;
using System.Security.Cryptography;
using CoenM.ImageHash.HashAlgorithms;
using CoenM.ImageHash;
using Microsoft.Extensions.Configuration;
using IS.ImageService.Api.Services.DeterminationService;
using Contracts.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using IS.ImageService.Api.Services.CacheService;
using Microsoft.AspNetCore.Authentication.JwtBearer;


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

        builder.Services.AddSingleton<ITaskPublisher, TaskPublisher>();
        builder.Services.AddSingleton<IFileDeterminator, FileDeterminator>();
        builder.Services.AddSingleton<ICacher, Cacher>();

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

        builder.AddRedisClient(connectionName: "rediscache");

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
