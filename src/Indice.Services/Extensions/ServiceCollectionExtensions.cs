using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class IndiceServicesServiceCollectionExtensions
    {
        /// <summary>
        /// Add a decorator pattern implementation.
        /// </summary>
        /// <typeparam name="TService">The service type to decorate.</typeparam>
        /// <typeparam name="TDecorator">The decorator.</typeparam>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddDecorator<TService, TDecorator>(this IServiceCollection services)
            where TService : class
            where TDecorator : class, TService {
            var serviceDescriptor = services.Where(x => x.ServiceType == typeof(TService)).LastOrDefault();
            if (serviceDescriptor is null) {
                services.AddTransient<TService, TDecorator>();
                return services;
            }
            services.TryAddTransient(serviceDescriptor.ImplementationType);
            return services.AddTransient<TService, TDecorator>(serviceProvider => {
                var parameters = typeof(TDecorator).GetConstructors(BindingFlags.Public | BindingFlags.Instance).First().GetParameters();
                var arguments = parameters.Select(x => x.ParameterType.Equals(typeof(TService)) ? serviceProvider.GetRequiredService(serviceDescriptor.ImplementationType) : serviceProvider.GetService(x.ParameterType)).ToArray();
                return (TDecorator)Activator.CreateInstance(typeof(TDecorator), arguments);
                //return ActivatorUtilities.CreateInstance<TDecorator>(serviceProvider, arguments);
            });
        }


    }
}
