using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.Core.Services.DataResolver;

/// <summary>
/// Resolves the <see cref="ICustomerDataResolver"/> to use for a given case type from keyed registrations,
/// falling back to the resolver registered under <see cref="CaseDataResolver.ServiceKey"/>.
/// </summary>
public sealed class CustomerDataResolverFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a new <see cref="CustomerDataResolverFactory"/>.</summary>
    public CustomerDataResolverFactory(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>Gets the resolver registered for <paramref name="caseType"/>, falling back to the default resolver.</summary>
    /// <param name="caseType">The case type code used as the keyed service key.</param>
    public ICustomerDataResolver Create(string? caseType) {
        if (!string.IsNullOrWhiteSpace(caseType)) {
            var resolver = _serviceProvider.GetKeyedService<ICustomerDataResolver>(caseType);
            if (resolver is not null) {
                return resolver;
            }
        }
        return _serviceProvider.GetRequiredKeyedService<ICustomerDataResolver>(CaseDataResolver.ServiceKey);
    }
}
