using Indice.AspNetCore.Swagger;
using Indice.Configuration;
using Indice.Features.Cases;

namespace Indice.Cases.Configuation

{
    /// <summary>
    /// Extension methods for setting up Swagger Gen services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class SwaggerConfig
    {
        /// <summary>
        /// Adds MVC services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services</param>
        /// <param name="settings"></param>
        /// <returns> An <see cref="IServiceCollection"/> that can be used to further configure dependencies.</returns>
        public static IServiceCollection AddSwaggerGenConfiguration(this IServiceCollection services, GeneralSettings settings)
        {
            return services.AddSwaggerGen(options => {
                options.IndiceDefaults(settings);
                options.AddOAuth2AuthorizationCodeFlow(settings);
                options.IncludeXmlComments(typeof(CasesApiConstants).Assembly);
            });
        }
    }
}
