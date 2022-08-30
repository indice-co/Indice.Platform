/* 
 * Attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution 
 */

using System;
using Indice.AspNetCore.MultiTenancy.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Indice.AspNetCore.MultiTenancy
{
    /// <summary>A builder used to configure the multi-tenancy feature.</summary>
    public class TenantBuilder<TTenant> where TTenant : Tenant
    {
        private readonly IServiceCollection _services;

        /// <summary>Constructs the <see cref="TenantBuilder{T}"/> class.</summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public TenantBuilder(IServiceCollection services) {
            if (typeof(TTenant).Equals(typeof(Tenant))) {
                services.AddTransient<TenantAccessService>();
            }
            services.AddTransient<TenantAccessService<TTenant>>();
            services.AddTransient<ITenantAccessor<TTenant>, TenantAccessor<TTenant>>();
            services.AddTransient<IAuthorizationHandler, BeTenantMemberHandler <TTenant>>();
            _services = services;
        }

        /// <summary>
        /// Register the tenant resolver implementation
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public TenantBuilder<TTenant> WithResolutionStrategy<V>(ServiceLifetime lifetime = ServiceLifetime.Transient) where V : class, ITenantResolutionStrategy {
            _services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            _services.Add(ServiceDescriptor.Describe(typeof(ITenantResolutionStrategy), typeof(V), lifetime));
            return this;
        }

        /// <summary>
        /// Register the tenant resolver implementation
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="implementationFactory"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public TenantBuilder<TTenant> WithResolutionStrategy<V>(Func<IServiceProvider, V> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Transient) where V : class, ITenantResolutionStrategy {
            _services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            _services.Add(ServiceDescriptor.Describe(typeof(ITenantResolutionStrategy), implementationFactory, lifetime));
            return this;
        }

        /// <summary>
        /// Register the tenant store implementation
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public TenantBuilder<TTenant> WithStore<V>(ServiceLifetime lifetime = ServiceLifetime.Transient) where V : class, ITenantStore<TTenant> {
            _services.Add(ServiceDescriptor.Describe(typeof(ITenantStore<TTenant>), typeof(V), lifetime));
            return this;
        }

        /// <summary>
        /// Register the tenant store implementation
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="implementationFactory"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public TenantBuilder<TTenant> WithStore<V>(Func<IServiceProvider, V> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Transient) where V : class, ITenantStore<TTenant> {
            _services.Add(ServiceDescriptor.Describe(typeof(ITenantStore<TTenant>), implementationFactory, lifetime));
            return this;
        }
    }
}
