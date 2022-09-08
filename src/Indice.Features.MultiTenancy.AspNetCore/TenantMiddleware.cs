using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.MultiTenancy.AspNetCore
{
    internal class TenantMiddleware<TTenant> where TTenant : Tenant
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next) {
            _next = next;
        }

        public async Task Invoke(HttpContext context) {
            if (!context.Items.ContainsKey(Constants.HttpContextTenantKey)) {
                var tenantService = context.RequestServices.GetService(typeof(TenantAccessService<TTenant>)) as TenantAccessService<TTenant>;
                context.Items.Add(Constants.HttpContextTenantKey, await tenantService.GetTenantAsync());
            }
            await _next?.Invoke(context);
        }
    }
}
