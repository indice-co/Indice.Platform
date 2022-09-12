using Indice.Features.Multitenancy.Worker.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Extension methods on type <see cref="IServiceCollection"/>.</summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>Adds the default implementation of <see cref="IFunctionContextAccessor"/>.</summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddFunctionContextAccessor(this IServiceCollection services) {
            services.TryAddSingleton<IFunctionContextAccessor, FunctionContextAccessor>();
            return services;
        }
    }
}
