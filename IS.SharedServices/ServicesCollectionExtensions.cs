using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace IS.SharedServices
{
    // This class is not for use now, but it can be used in the future to add shared services to the DI container
    public static class ServicesCollectionExtensions
    {
        //public static IServiceCollection AddSharedServices(
        //  this IServiceCollection services,
        //  IConfiguration config)
        //{
        //    services.AddSingleton<ITaskPublisher, TaskPublisher>();
        //    services.AddSingleton<ICacher, Cacher>();

        //    return services;
        //}
        //public static IServiceCollection AddTaskPublisher(
        //  this IServiceCollection services,
        //  IConfiguration config)
        //{
        //    services.AddSingleton<ITaskPublisher, TaskPublisher>();

        //    return services;
        //}
    }
}
