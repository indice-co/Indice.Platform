using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Http.Filters;

/// <summary>Extension methods on <see cref="IEndpointConventionBuilder"/> for validating antiforgery token.</summary>
public static class AntiforgeryExtensions
{
    /// <summary>Adds an endpoint filter for validating antiforgery tokens.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    public static TBuilder ValidateAntiforgery<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder {
        return builder.AddEndpointFilter(async (context, next) => {
            try {
                var antiForgeryService = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
                await antiForgeryService.ValidateRequestAsync(context.HttpContext);
            } catch (AntiforgeryValidationException) {
                var errors = ValidationErrors.Create();
                errors.AddError("antiforgery", "Antiforgery token validation failed.");
                return Results.ValidationProblem(errors, detail: "Request not valid.");
            }
            return await next(context);
        });
    }
}