using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
    /// <param name="prefix">The route pattern used to receive webhook requests.</param>
    /// <returns>An endpoint convention builder for the mapped webhook endpoint.</returns>
    public static IEndpointConventionBuilder MapWebhooks(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string prefix = "/webhooks") 
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Webhooks")
            .WithSummary("Webhook endpoints")
            .WithDescription("Endpoints for receiving and processing webhook notifications from external providers.")
            .AllowAnonymous();

        group.ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
            
        group.MapPost("{provider}", HandleWebhook)
            .WithName("ReceiveWebhook")
            .WithSummary("Receive a webhook notification")
            .WithDescription(
                """
                Receives a webhook notification from the specified provider, validates the request,
                and processes the notification using the provider-specific webhook handler.
                """)
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        return group;
    }

    internal static async Task<Results<ValidationProblem, NotFound, NoContent>> HandleWebhook( 
        string provider, HttpContext context, Channel<List<WebhookEvent>> channel, CancellationToken cancellationToken) {
            var webhooksHandler = context.RequestServices.GetService<IWebhookHttpHandler>();
            if (webhooksHandler is null) {
                return TypedResults.NotFound();
            }

            var result = await webhooksHandler.ParseAsync(context.Request.Headers, context.Request.Query, context.Request.Body, cancellationToken);
            if (!result.Success) {
                return result.Status switch {
                    >= 200 and <= 204 => TypedResults.NoContent(),
                    >= 400 and <= 403 => TypedResults.ValidationProblem(
                        ValidationErrors.AddErrors(provider, result.Errors)),
                    _ => TypedResults.ValidationProblem(
                        ValidationErrors.AddErrors(provider, result.Errors))
                };
            }

            if (result.Events.Any()) {
                await channel.Writer.WriteAsync(result.Events, cancellationToken);
            }

            return TypedResults.NoContent();
    }
}
