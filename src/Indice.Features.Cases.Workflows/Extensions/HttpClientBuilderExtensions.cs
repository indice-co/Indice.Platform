using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Indice.Features.Cases.Workflows.Extensions;

// TODO: Platform team will move this to Builder, also remove Microsoft.Extensions.Http.Resilience dependency
/// <summary>Provider extension methods to <see cref="IHttpClientBuilder"/></summary>
public static class HttpClientBuilderExtensions
{
#pragma warning disable EXTEXP0001
    /// <summary>
    /// Remove already configured resilience handlers
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static IHttpClientBuilder ClearResilienceHandlers(this IHttpClientBuilder builder) {
        builder.ConfigureAdditionalHttpMessageHandlers((handlers, _) => {
            for (var i = 0; i < handlers.Count;) {
                if (handlers[i] is ResilienceHandler) {
                    handlers.RemoveAt(i);
                    continue;
                }

                i++;
            }
        });
        return builder;
    }
#pragma warning restore EXTEXP0001
}