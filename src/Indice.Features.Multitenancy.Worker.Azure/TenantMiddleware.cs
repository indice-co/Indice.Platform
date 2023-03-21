using Indice.Features.Multitenancy.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Indice.Features.Multitenancy.Worker.Azure;

internal class TenantMiddleware<TTenant> : IFunctionsWorkerMiddleware where TTenant : Tenant
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next) {
        if (!context.Items.ContainsKey(Constants.FunctionContextTenantKey)) {
            var tenantService = context.InstanceServices.GetService(typeof(TenantAccessService<TTenant>)) as TenantAccessService<TTenant>;
            context.Items.Add(Constants.FunctionContextTenantKey, await tenantService.GetTenantAsync());
        }
        await next(context);
    }
}
