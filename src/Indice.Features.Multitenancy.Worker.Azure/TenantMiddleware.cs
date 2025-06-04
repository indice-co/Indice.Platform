using Indice.Features.Multitenancy.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Multitenancy.Worker.Azure;

internal class TenantMiddleware<TTenant> : IFunctionsWorkerMiddleware where TTenant : Tenant
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next) {
        if (!context.Items.ContainsKey(Constants.FunctionContextTenantKey)) {
            var tenantService = context.InstanceServices.GetRequiredService<TenantAccessService<TTenant>>();
            context.Items.Add(Constants.FunctionContextTenantKey, (await tenantService.GetTenantAsync())!);
        }
        await next(context);
    }
}
