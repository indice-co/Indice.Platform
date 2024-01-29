#if !NET8_0_OR_GREATER

namespace Microsoft.Extensions.DependencyInjection;

// TODO: Rmove this for net8.0 onwards with the native implementation that is better.

/// <summary>Used in order to register named implementations, factories &amp; instances.</summary>
/// <remarks>Taken from this post https://stackoverflow.com/a/67154320/61577 </remarks>
public static class KeyedServiceExtensions
{
    #region polyfils
    #nullable enable
    /// <summary>
    /// Adds a transient service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The ServiceKey of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="ServiceLifetime.Transient"/>
    public static IServiceCollection AddKeyedTransient<TService, TImplementation>(
        this IServiceCollection services,
        object? serviceKey)
        where TService : class
        where TImplementation : class, TService {

        return AddKeyedService<TService, TImplementation, object?>(services, key: serviceKey, serviceLifetime: ServiceLifetime.Transient);
    }
    /// <summary>
    /// Adds a transient service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation" /> using the
    /// factory specified in <paramref name="implementationFactory"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The ServiceKey of the service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="ServiceLifetime.Transient"/>
    public static IServiceCollection AddKeyedTransient<TService, TImplementation>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, object?, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService {

        return AddKeyedService<TService, TImplementation, object?>(services, key: serviceKey, implementationFactory: sp => implementationFactory(sp, serviceKey), serviceLifetime: ServiceLifetime.Transient);
    }

    /// <summary>
    /// Adds a transient service of the type specified in <typeparamref name="TService"/> with a
    /// factory specified in <paramref name="implementationFactory"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The ServiceKey of the service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="ServiceLifetime.Transient"/>
    public static IServiceCollection AddKeyedTransient<TService>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class {

        return AddKeyedService<TService, TService, object?>(services, key: serviceKey, implementationFactory: sp => implementationFactory(sp, serviceKey), serviceLifetime: ServiceLifetime.Transient);
    }
    /// <summary>
    /// Adds a Scoped service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The ServiceKey of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="ServiceLifetime.Scoped"/>
    public static IServiceCollection AddKeyedScoped<TService, TImplementation>(
        this IServiceCollection services,
        object? serviceKey)
        where TService : class
        where TImplementation : class, TService {

        return AddKeyedService<TService, TImplementation, object?>(services, key: serviceKey, serviceLifetime: ServiceLifetime.Scoped);
    }
    /// <summary>
    /// Adds a Scoped service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation" /> using the
    /// factory specified in <paramref name="implementationFactory"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The ServiceKey of the service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="ServiceLifetime.Scoped"/>
    public static IServiceCollection AddKeyedScoped<TService, TImplementation>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, object?, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService {

        return AddKeyedService<TService, TImplementation, object?>(services, key: serviceKey, implementationFactory: sp => implementationFactory(sp, serviceKey), serviceLifetime: ServiceLifetime.Scoped);
    }

    /// <summary>
    /// Adds a Scoped service of the type specified in <typeparamref name="TService"/> with a
    /// factory specified in <paramref name="implementationFactory"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The ServiceKey of the service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="ServiceLifetime.Scoped"/>
    public static IServiceCollection AddKeyedScoped<TService>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class {

        return AddKeyedService<TService, TService, object?>(services, key: serviceKey, implementationFactory: sp => implementationFactory(sp, serviceKey), serviceLifetime: ServiceLifetime.Scoped);
    }
    /// <summary>
    /// Adds a Singleton service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The ServiceKey of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="ServiceLifetime.Singleton"/>
    public static IServiceCollection AddKeyedSingleton<TService, TImplementation>(
        this IServiceCollection services,
        object? serviceKey)
        where TService : class
        where TImplementation : class, TService {

        return AddKeyedService<TService, TImplementation, object?>(services, key: serviceKey, serviceLifetime: ServiceLifetime.Singleton);
    }
    /// <summary>
    /// Adds a Singleton service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation" /> using the
    /// factory specified in <paramref name="implementationFactory"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The ServiceKey of the service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="ServiceLifetime.Scoped"/>
    public static IServiceCollection AddKeyedSingleton<TService, TImplementation>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, object?, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService {

        return AddKeyedService<TService, TImplementation, object?>(services, key: (string)serviceKey!, implementationFactory: sp => implementationFactory(sp, serviceKey), serviceLifetime: ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds a Singleton service of the type specified in <typeparamref name="TService"/> with a
    /// factory specified in <paramref name="implementationFactory"/> to the
    /// specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceKey">The ServiceKey of the service.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="ServiceLifetime.Singleton"/>
    public static IServiceCollection AddKeyedSingleton<TService>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class {

        return AddKeyedService<TService, TService, object?>(services, key: (string)serviceKey!, implementationFactory: sp => implementationFactory(sp, serviceKey), serviceLifetime: ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
    /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
    /// <returns>A service object of type <typeparamref name="T"/> or null if there is no such service.</returns>
    public static T? GetKeyedService<T>(this IServiceProvider provider, object? serviceKey) { 
        var factory = provider.GetService<Func<object?, T>>();
        return factory is null ? default(T) : factory.Invoke(serviceKey);
    }


    /// <summary>
    /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
    /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
    /// <returns>A service object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T"/>.</exception>
    public static T GetRequiredKeyedService<T>(this IServiceProvider provider, object? serviceKey) where T : notnull {
        var factory = provider.GetRequiredService<Func<object?, T>>();
        return factory.Invoke(serviceKey);
    }

    /// <summary>
    /// Get an enumeration of services of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the services from.</param>
    /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
    /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
    public static IEnumerable<T> GetKeyedServices<T>(this IServiceProvider provider, object? serviceKey) {
        var factories = provider.GetServices<Func<object?, T>>();
        return factories.Select(x => x.Invoke(serviceKey));
    }
#nullable disable
    #endregion

    /// <summary>Use this to register <typeparamref name="TImplementation"/> as <typeparamref name="TService"/>, injectable as <see cref="Func{TKey, TService}"/>. Uses default instance activator.</summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="key"></param>
    /// <param name="serviceLifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    internal static IServiceCollection AddKeyedService<TService, TImplementation, TKey>(this IServiceCollection services, TKey key, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : class, TService {
        services.AddTransient<TImplementation>();
        var keyedServiceBuilder = services.CreateOrUpdateKeyedServiceBuilder<TKey, TService, Func<TKey, TService>>(DefaultImplementationFactory<TKey, TService>, serviceLifetime);
        keyedServiceBuilder.Add<TImplementation>(key);
        return services;
    }

    /// <summary>Use this to register <typeparamref name="TImplementation"/> as <typeparamref name="TService"/>, injectable as <see cref="Func{TKey, TService}"/>.</summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="key"></param>
    /// <param name="implementationFactory"></param>
    /// <param name="serviceLifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    internal static IServiceCollection AddKeyedService<TService, TImplementation, TKey>(this IServiceCollection services, TKey key, Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : class, TService {
        services.AddTransient(implementationFactory);
        var keyedServiceBuilder = services.CreateOrUpdateKeyedServiceBuilder<TKey, TService, Func<TKey, TService>>(DefaultImplementationFactory<TKey, TService>, serviceLifetime);
        keyedServiceBuilder.Add<TImplementation>(key);
        return services;
    }

    /// <summary>Use this to register <typeparamref name="TImplementation"/> as <typeparamref name="TService"/>, injectable as <typeparamref name="TInjection"/>. Uses default instance activator.</summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TInjection"></typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="key"></param>
    /// <param name="serviceFactory"></param>
    /// <param name="serviceLifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    internal static IServiceCollection AddKeyedService<TService, TImplementation, TKey, TInjection>(this IServiceCollection services, TKey key, Func<IServiceProvider, Func<TKey, TService>, TInjection> serviceFactory, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : class, TService
        where TInjection : class {
        services.AddTransient<TImplementation>();
        var keyedServiceBuilder = services.CreateOrUpdateKeyedServiceBuilder<TKey, TService, TInjection>(x => serviceFactory(x, DefaultImplementationFactory<TKey, TService>(x)), serviceLifetime);
        keyedServiceBuilder.Add<TImplementation>(key);
        return services;
    }

    /// <summary>Use this to register <typeparamref name="TImplementation"/> as <typeparamref name="TService"/>, injectable as <typeparamref name="TInjection"/>. Uses implementationFactory to create instances.</summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TInjection"></typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="key"></param>
    /// <param name="implementationFactory"></param>
    /// <param name="serviceFactory"></param>
    /// <param name="serviceLifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    internal static IServiceCollection AddKeyedService<TService, TImplementation, TKey, TInjection>(this IServiceCollection services, TKey key, Func<IServiceProvider, TImplementation> implementationFactory, Func<IServiceProvider, Func<TKey, TService>, TInjection> serviceFactory, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : class, TService
        where TInjection : class {
        services.AddTransient(implementationFactory);
        var keyedServiceBuilder = services.CreateOrUpdateKeyedServiceBuilder<TKey, TService, TInjection>(x => serviceFactory(x, DefaultImplementationFactory<TKey, TService>(x)), serviceLifetime);
        keyedServiceBuilder.Add<TImplementation>(key);
        return services;
    }

    private static KeyedServiceBuilder<TKey, TService> CreateOrUpdateKeyedServiceBuilder<TKey, TService, TInjection>(this IServiceCollection services, Func<IServiceProvider, TInjection> serviceFactory, ServiceLifetime serviceLifetime)
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
            if (actual != expected) {
                throw new ApplicationException($"{typeof(T).FullName} is already registered with a different ServiceLifetime. Expected: '{expected}', Actual: '{actual}'");
            }
        }
    }

    private static Func<TKey, TService> DefaultImplementationFactory<TKey, TService>(IServiceProvider x) where TService : class => x.GetRequiredService<KeyedServiceBuilder<TKey, TService>>().Build(x);

    private sealed class KeyedServiceBuilder<TKey, TService>
    {
        private readonly Dictionary<TKey, Type> _serviceImplementationTypes = new Dictionary<TKey, Type>();

        internal void Add<TImplementation>(TKey key) where TImplementation : class, TService {
            if (_serviceImplementationTypes.TryGetValue(key, out var type) && type == typeof(TImplementation)) {
                return; // This type is already registered under this key.
            }
            _serviceImplementationTypes[key] = typeof(TImplementation);
        }

        internal Func<TKey, TService> Build(IServiceProvider serviceProvider) {
            var serviceTypeDictionary = _serviceImplementationTypes
                .Values
                .Distinct()
                .ToDictionary(
                    type => type,
                    type => new Lazy<TService>(() => (TService)serviceProvider.GetRequiredService(type), LazyThreadSafetyMode.ExecutionAndPublication)
                );
            var serviceDictionary = _serviceImplementationTypes.ToDictionary(kvp => kvp.Key, kvp => serviceTypeDictionary[kvp.Value]);
            return key => serviceDictionary[key].Value;
        }
    }
}
#endif