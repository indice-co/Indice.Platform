using Indice.Features.Multitenancy.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Multitenancy.AspNetCore;

internal class TenantMiddleware<TTenant> where TTenant : Tenant
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) {
        _next = next;
    }

    public async Task Invoke(HttpContext context) {
        if (!context.Items.ContainsKey(Constants.HttpContextTenantKey)) {
            var tenantService = context.RequestServices.GetRequiredService<TenantAccessService<TTenant>>();
            context.Items.Add(Constants.HttpContextTenantKey, await tenantService.GetTenantAsync());
        }
        await _next?.Invoke(context)!;
    }
}
