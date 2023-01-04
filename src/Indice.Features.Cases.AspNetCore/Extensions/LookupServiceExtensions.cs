using Indice.Features.Cases.Interfaces;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LookupServiceExtensions
    {
        public static void AddCaseLookupService<TLookupService>(this IServiceCollection services)
            where TLookupService : class, ILookupService {
            services.AddTransient<ILookupService, TLookupService>();
        }
    }
}
