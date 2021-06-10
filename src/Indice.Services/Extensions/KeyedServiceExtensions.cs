using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Used in order to register named implementations, factories &amp; instances
    /// </summary>
    /// <remarks>taken from this SO post https://stackoverflow.com/a/67154320/61577 </remarks>
    public static class KeyedServiceExtensions
    {
        /// <summary>
        /// Use this to register TImplementation as TService, injectable as <see cref="Func{TKey, TService}"/>.
        /// Uses default instance activator.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="services"></param>
        /// <param name="key"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddKeyedService<TService, TImplementation, TKey>(this IServiceCollection services, TKey key, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService {
            services.AddTransient<TImplementation>();
            var keyedServiceBuilder = services.CreateOrUpdateKeyedServiceBuilder<TKey, TService, Func<TKey, TService>>(
                DefaultImplementationFactory<TKey, TService>, serviceLifetime);
            keyedServiceBuilder.Add<TImplementation>(key);

            return services;
        }

        /// <summary>
        /// Use this to register <typeparamref name="TImplementation"/> as <typeparamref name="TService"/>, injectable as <see cref="Func{TKey, TService}"/>
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="services"></param>
        /// <param name="key"></param>
        /// <param name="implementationFactory"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddKeyedService<TService, TImplementation, TKey>(this IServiceCollection services, TKey key,
            Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService {
            services.AddTransient(implementationFactory);

            var keyedServiceBuilder = services.CreateOrUpdateKeyedServiceBuilder<TKey, TService, Func<TKey, TService>>(
                DefaultImplementationFactory<TKey, TService>, serviceLifetime);
            keyedServiceBuilder.Add<TImplementation>(key);

            return services;
        }

        /// <summary>
        /// Use this to register <typeparamref name="TImplementation"/> as <typeparamref name="TService"/>, injectable as <typeparamref name="TInjection"/>
        /// Uses default instance activator.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TInjection"></typeparam>
        /// <param name="services"></param>
        /// <param name="key"></param>
        /// <param name="serviceFactory"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddKeyedService<TService, TImplementation, TKey, TInjection>(this IServiceCollection services, TKey key,
            Func<IServiceProvider, Func<TKey, TService>, TInjection> serviceFactory, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
            where TInjection : class {
            services.AddTransient<TImplementation>();

            var keyedServiceBuilder = services.CreateOrUpdateKeyedServiceBuilder<TKey, TService, TInjection>(
                x => serviceFactory(x, DefaultImplementationFactory<TKey, TService>(x)), serviceLifetime);
            keyedServiceBuilder.Add<TImplementation>(key);

            return services;
        }

        /// <summary>
        /// Use this to register TImplementation as TService, injectable as TInjection. 
        /// Uses implementationFactory to create instances
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TInjection"></typeparam>
        /// <param name="services"></param>
        /// <param name="key"></param>
        /// <param name="implementationFactory"></param>
        /// <param name="serviceFactory"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddKeyedService<TService, TImplementation, TKey, TInjection>(this IServiceCollection services, TKey key,
            Func<IServiceProvider, TImplementation> implementationFactory, Func<IServiceProvider, Func<TKey, TService>, TInjection> serviceFactory, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
            where TInjection : class {
            services.AddTransient(implementationFactory);

            var keyedServiceBuilder = services.CreateOrUpdateKeyedServiceBuilder<TKey, TService, TInjection>(
                x => serviceFactory(x, DefaultImplementationFactory<TKey, TService>(x)), serviceLifetime);
            keyedServiceBuilder.Add<TImplementation>(key);

            return services;
        }

        private static KeyedServiceBuilder<TKey, TService> CreateOrUpdateKeyedServiceBuilder<TKey, TService, TInjection>(this IServiceCollection services,
            Func<IServiceProvider, TInjection> serviceFactory, ServiceLifetime serviceLifetime)
            where TService : class
            where TInjection : class {
            var builderServiceDescription = services.SingleOrDefault(x => x.ServiceType == typeof(KeyedServiceBuilder<TKey, TService>));
            KeyedServiceBuilder<TKey, TService> keyedServiceBuilder;
            if (builderServiceDescription is null) {
                keyedServiceBuilder = new KeyedServiceBuilder<TKey, TService>();
                services.AddSingleton(keyedServiceBuilder);

                switch (serviceLifetime) {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(serviceFactory);
                        break;
                    case ServiceLifetime.Scoped:
                        services.AddScoped(serviceFactory);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(serviceFactory);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, "Invalid value for " + nameof(serviceLifetime));
                }
            } else {
                CheckLifetime<KeyedServiceBuilder<TKey, TService>>(builderServiceDescription.Lifetime, ServiceLifetime.Singleton);

                var factoryServiceDescriptor = services.SingleOrDefault(x => x.ServiceType == typeof(TInjection));
                CheckLifetime<TInjection>(factoryServiceDescriptor.Lifetime, serviceLifetime);

                keyedServiceBuilder = (KeyedServiceBuilder<TKey, TService>)builderServiceDescription.ImplementationInstance;
            }

            return keyedServiceBuilder;

            static void CheckLifetime<T>(ServiceLifetime actual, ServiceLifetime expected) {
                if (actual != expected)
                    throw new ApplicationException($"{typeof(T).FullName} is already registered with a different ServiceLifetime. Expected: '{expected}', Actual: '{actual}'");
            }
        }

        private static Func<TKey, TService> DefaultImplementationFactory<TKey, TService>(IServiceProvider x) where TService : class
            => x.GetRequiredService<KeyedServiceBuilder<TKey, TService>>().Build(x);

        private sealed class KeyedServiceBuilder<TKey, TService>
        {
            private readonly Dictionary<TKey, Type> _serviceImplementationTypes = new Dictionary<TKey, Type>();

            internal void Add<TImplementation>(TKey key) where TImplementation : class, TService {
                if (_serviceImplementationTypes.TryGetValue(key, out var type) && type == typeof(TImplementation))
                    return; //this type is already registered under this key

                _serviceImplementationTypes[key] = typeof(TImplementation);
            }

            internal Func<TKey, TService> Build(IServiceProvider serviceProvider) {
                var serviceTypeDictionary = _serviceImplementationTypes.Values.Distinct()
                    .ToDictionary(
                        type => type,
                        type => new Lazy<TService>(
                            () => (TService)serviceProvider.GetRequiredService(type),
                            LazyThreadSafetyMode.ExecutionAndPublication
                        )
                    );
                var serviceDictionary = _serviceImplementationTypes
                    .ToDictionary(kvp => kvp.Key, kvp => serviceTypeDictionary[kvp.Value]);

                return key => serviceDictionary[key].Value;
            }
        }
    }

}
