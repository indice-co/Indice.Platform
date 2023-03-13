using Azure;
using Indice.Extensions;
using Xunit;

namespace Indice.Services.Tests;

public class BlobPropertiesExtensionsTest
{
    [Theory]
    [InlineData("0x8D979DAB44F6E71")]
    [InlineData("\"0x8D979DAB44F6E71\"")]
    public void EtagTest(string input) {
        ETag sourceEtag = new ETag(input);
        var etag = sourceEtag.GetHttpSafeETag();
        Assert.NotNull(etag);
        Assert.StartsWith("\"", etag);
        Assert.EndsWith("\"", etag);
    }
}
