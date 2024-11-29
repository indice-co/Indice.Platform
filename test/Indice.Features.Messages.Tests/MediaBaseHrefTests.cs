using Indice.Features.Messages.Core.Models.Requests;
using Xunit;

namespace Indice.Features.Messages.Tests;

public class MediaBaseHrefTests
{
    [Fact]
    public void MediaBaseHref_CreatesAbsoluteFromRelative_Url() {
        var applicationHost = "https://messaging";
        var request = new CreateCampaignRequest() {
            MediaBaseHref = "/api/media-root"
        };
        if (Uri.TryCreate(request!.MediaBaseHref, UriKind.RelativeOrAbsolute, out var mediaBaseUri) && !mediaBaseUri.IsAbsoluteUri) {
            var baseUri = new Uri(applicationHost);
            request.MediaBaseHref = new Uri(baseUri, mediaBaseUri).ToString();
        }
        Assert.Equal("https://messaging/api/media-root", request.MediaBaseHref);
    }
}
