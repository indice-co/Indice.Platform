using Indice.AspNetCore.Identity.Filters;
using Indice.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Swagger configuration extensions the Indice way. Exposes useful defaults for hosting an API.</summary>
    public static class SwaggerConfig
    {
        /// <summary>Adds an operation filter that add a suitable header field in the endpoints that use the <see cref="RequiresOtpAttribute"/>.</summary>
        /// <param name="options">Options for configuring Swagger.</param>
        /// <param name="headerName">The header value for capturing the TOTP code.</param>
        public static SwaggerGenOptions AddRequiresOtpOperationFilter(this SwaggerGenOptions options, string headerName = RequiresOtpAttribute.DEFAULT_HEADER_NAME) {
            options.OperationFilter<RequiresOtpOperationFilter>(headerName);
            return options;
        }
    }
}
