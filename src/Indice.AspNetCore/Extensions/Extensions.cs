using Indice.Abstractions;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Extensions
{
    public static class Extensions
    {
        public static IServiceCollection AddIndiceServices(this IServiceCollection services) {
            services.AddTransient<IMarkdownProcessor, MarkdigProcessor>();
            return services;
        }
    }
}
