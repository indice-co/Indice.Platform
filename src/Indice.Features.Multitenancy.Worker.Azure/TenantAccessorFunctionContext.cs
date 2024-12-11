using Indice.Features.Multitenancy.Core;
using Microsoft.Azure.Functions.Worker;

namespace Indice.Features.Multitenancy.Worker.Azure;

/// <summary>This version of the <see cref="ITenantAccessor{TTenant}"/> is based on the <seealso cref="FunctionContext"/> in order to figure out the current tenant.</summary>
/// <typeparam name="TTenant"></typeparam>
public class TenantAccessorFunctionContext<TTenant> : ITenantAccessor<TTenant> where TTenant : Tenant
{
    private readonly IFunctionContextAccessor _functionContextAccessor;

    /// <inheritdoc/>
    public TenantAccessorFunctionContext(IFunctionContextAccessor functionContextAccessor) {
        _functionContextAccessor = functionContextAccessor ?? throw new ArgumentNullException(nameof(functionContextAccessor));
    }

    /// <inheritdoc/>
    public TTenant? Tenant => _functionContextAccessor.FunctionContext?.GetTenant<TTenant>()!;
}
