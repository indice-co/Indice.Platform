using System;
using Indice.Features.Multitenancy.Core;
using Microsoft.Azure.Functions.Worker;

namespace Indice.Features.Multitenancy.Worker.Azure
{
    internal class TenantAccessorFunctionContext<TTenant> : ITenantAccessor<TTenant> where TTenant : Tenant
    {
        private readonly IFunctionContextAccessor _functionContextAccessor;

        public TenantAccessorFunctionContext(IFunctionContextAccessor functionContextAccessor) {
            _functionContextAccessor = functionContextAccessor ?? throw new ArgumentNullException(nameof(functionContextAccessor));
        }

        public TTenant Tenant => _functionContextAccessor.FunctionContext.GetTenant<TTenant>();
    }
}
