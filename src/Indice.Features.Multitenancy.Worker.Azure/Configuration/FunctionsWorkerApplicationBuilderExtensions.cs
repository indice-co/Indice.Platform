using Indice.Features.Multitenancy.Core;
using Indice.Features.Multitenancy.Worker.Azure;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker;

/// <summary>Extension methods on <see cref="IFunctionsWorkerApplicationBuilder"/> to register custom middleware.</summary>
public static class FunctionsWorkerApplicationBuilderExtensions
{
    /// <summary>Registers the <see cref="TenantMiddleware{TTenant}"/> middleware.</summary>
    /// <typeparam name="TTenant">The type of the tenant.</typeparam>
    /// <param name="builder">Represents a builder for a Functions Worker Application.</param>
    public static IFunctionsWorkerApplicationBuilder UseMultiTenancy<TTenant>(this IFunctionsWorkerApplicationBuilder builder) where TTenant : Tenant => builder.UseMiddleware<TenantMiddleware<TTenant>>();

    /// <summary>Registers the <see cref="TenantMiddleware{TTenant}"/> middleware.</summary>
    /// <param name="builder">Represents a builder for a Functions Worker Application.</param>
    public static IFunctionsWorkerApplicationBuilder UseMultiTenancy(this IFunctionsWorkerApplicationBuilder builder) => builder.UseMiddleware<TenantMiddleware<Tenant>>();

    /// <summary>Registers the <see cref="FunctionContextAccessorMiddleware"/> middleware.</summary>
    /// <param name="builder">Represents a builder for a Functions Worker Application.</param>
    public static IFunctionsWorkerApplicationBuilder UseFunctionContextAccessor(this IFunctionsWorkerApplicationBuilder builder) => builder.UseMiddleware<FunctionContextAccessorMiddleware>();
}
