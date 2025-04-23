using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Indice.Features.Cases.Workflows.Extensions;

// TODO: Platform team will move this to Builder, also remove Microsoft.Extensions.Http.Resilience dependency, merged in 9 https://github.com/dotnet/extensions/pull/5801/files
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
        builder.ConfigureAdditionalHttpMessageHandlers(static (handlers, _) =>
        {
            for (var i = handlers.Count - 1; i >= 0; i--) {
                if (handlers[i] is ResilienceHandler) {
                    handlers.RemoveAt(i);
                }
            }
        });
        
        return builder;
    }
#pragma warning restore EXTEXP0001
}