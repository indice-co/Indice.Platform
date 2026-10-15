using System.Threading.Channels;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Middleware;

/// <summary>
/// Provides endpoint mapping extensions for webhook integrations.
/// </summary>
public static class WebhooksEndpointExtensions
{
    /// <summary>
    /// Maps an HTTP POST endpoint for receiving and processing webhook events.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern used to receive webhook requests.</param>
    /// <returns>An endpoint convention builder for the mapped webhook endpoint.</returns>
    public static IEndpointConventionBuilder MapWebhooks(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/webhooks/{provider}") 
    {
        return endpoints.MapPost(pattern, async (HttpContext context, Channel<List<WebhookEvent>> channel, CancellationToken cancellationToken) => {
            var provider = context.Request.RouteValues["provider"]?.ToString();
            if (string.IsNullOrWhiteSpace(provider)) {
                return Results.NotFound();
            }
            var webhooksHandler = context.RequestServices.GetService<IWebhookHttpHandler>();
            if (webhooksHandler is null) {
                return Results.NotFound();
            }
            var result = await webhooksHandler.ParseAsync(context.Request.Headers, context.Request.Query, context.Request.Body, cancellationToken);
            if (!result.Success) {
                return Results.Json(new { errors = result.Errors }, statusCode: result.Status);
            }
            if (result.Events.Any()) {
                await channel.Writer.WriteAsync(result.Events, cancellationToken);
            }
            return Results.StatusCode(result.Status);
        });
    }
}
