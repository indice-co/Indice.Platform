using Elsa.Activities.Http;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;

namespace Indice.Features.Cases.Workflows.Bookmarks;

/// <summary>
/// The Bookmark provider to be invoked when Elsa indexes workflows when they get suspended.
/// <remarks>See <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities#bookmarks">Elsa Bookmarks documentation</a></remarks>
/// <remarks>See <a href="https://github.com/tomy2105/elsa-core/blob/test/HTTPEndpointExtension/src/samples/dashboard/aspnetcore/ElsaDashboard.Samples.AspNetCore.Monolith/TestReceiveRequest.cs">Enhance HTTP endpoint</a>.</remarks>
/// </summary>
public class HttpEndpointWithValidationBookmarkProvider : BookmarkProvider<HttpEndpointBookmark, HttpEndpointWithValidation>
{
    /// <summary>
    /// Creates a new <see cref="HttpEndpointBookmark"/> from the context.
    /// </summary>
    public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<HttpEndpointWithValidation> context, CancellationToken cancellationToken) {
        var path = ToLower((await context.ReadActivityPropertyAsync(x => x.Path, cancellationToken))!);
        var methods = (await context.ReadActivityPropertyAsync(x => x.Methods, cancellationToken))?.Select(ToLower) ?? Enumerable.Empty<string>();

        BookmarkResult CreateBookmark(string? method) => Result(new(path!, method), nameof(HttpEndpoint));
        return methods.Select(CreateBookmark);
    }

    private static string? ToLower(string s) => s?.ToLowerInvariant();
}