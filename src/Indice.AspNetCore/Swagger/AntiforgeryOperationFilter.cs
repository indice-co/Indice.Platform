using System.Reflection;
using Indice.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger;

/// <summary>A filter that generates a corresponding header for action methods that require an antiforgery token as indicated by <see cref="ValidateAntiForgeryTokenAttribute"/>.</summary>
public class AntiforgeryOperationFilter : IOperationFilter
{
    /// <summary>Creates a new instance of <see cref="AntiforgeryOperationFilter"/>.</summary>
    /// <param name="xsrf">Provides access to the antiforgery system, which provides protection against Cross-site Request Forgery (XSRF, also called CSRF) attacks.</param>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    public AntiforgeryOperationFilter(IAntiforgery xsrf, IHttpContextAccessor httpContextAccessor) {
        Xsrf = xsrf ?? throw new ArgumentNullException(nameof(xsrf));
        HttpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>Provides access to the antiforgery system, which provides protection against Cross-site Request Forgery (XSRF, also called CSRF) attacks.</summary>
    public IAntiforgery Xsrf { get; }
    /// <summary>Provides access to the current HTTP context.</summary>
    public IHttpContextAccessor HttpContextAccessor { get; }

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context) {
        if (operation.Parameters == null) {
            operation.Parameters = new List<OpenApiParameter>();
        }
        var canGetMethodInfo = context.ApiDescription.TryGetMethodInfo(out var methodInfo);
        if (!canGetMethodInfo) {
            return;
        }
        var actionAttributes = methodInfo.GetCustomAttributes();
        var requiresSubscription = actionAttributes.SingleOrDefault(x => x.GetType() == typeof(ValidateAntiForgeryTokenAttribute)) != null;
        if (!requiresSubscription) {
            return;
        }
        operation.Parameters.Add(new OpenApiParameter {
            In = ParameterLocation.Header,
            Name = CustomHeaderNames.AntiforgeryHeaderName,
            Schema = new OpenApiSchema {
                Type = nameof(String),
                Default = new OpenApiString(Xsrf.GetAndStoreTokens(HttpContextAccessor.HttpContext).RequestToken)
            }
        });
    }
}
