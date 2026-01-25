using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var rmqpassword = builder.AddParameter("MQ-PASS", secret: true);
var rmquser = builder.AddParameter("MQ-USER", secret: true);

var storageHostPath = builder.AddParameter("HOST-PATH", secret: false);

var rabbitmq = builder.AddRabbitMQ("broker", password: rmqpassword, userName: rmquser)
    .WithDataVolume(isReadOnly: false)
    .WithManagementPlugin();

var postgresServer = builder.AddPostgres("postgresImageServer")
    .WithDataVolume(isReadOnly: false)
    .WithPgAdmin(pgadmin => pgadmin.WithHostPort(5050))
    .AddDatabase("imagesdb");

var redisCache = builder.AddRedis("rediscache")
    .WithDataVolume(isReadOnly: false);

builder.AddProject<Projects.IS_ImageService_Api>("api")
    .WithReference(rabbitmq)
    .WithReference(postgresServer)
    .WithReference(redisCache)
    .WithEnvironment("MQ-PASS", rmqpassword)
    .WithEnvironment("MQ-USER", rmquser)
    .WithEnvironment("Storage__Root", storageHostPath);

builder.AddProject<Projects.IS_ImageService_Worker>("worker")
    .WithReference(rabbitmq)
    .WithReference(postgresServer)
    .WithReference(redisCache)
    .WithEnvironment("MQ-PASS", rmqpassword)
    .WithEnvironment("MQ-USER", rmquser)
    .WithEnvironment("Storage__Root", storageHostPath);


builder.Build().Run();
