using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Signapse.Server
{
    internal static class ServiceExtensions
    {
        public static IServiceCollection UniqueAddTransient<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            if (services.Any(s => s.ServiceType == typeof(TService)) == false)
            {
                services.AddTransient<TService, TImplementation>();
            }

            return services;
        }

        public static IServiceCollection UniqueAddTransient<TService>(this IServiceCollection services)
            where TService : class
        {
            if (services.Any(s => s.ServiceType == typeof(TService)) == false)
            {
                services.AddTransient<TService>();
            }

            return services;
        }
    }
}
