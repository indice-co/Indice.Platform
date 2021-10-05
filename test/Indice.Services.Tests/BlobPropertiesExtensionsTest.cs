using System.Threading.Tasks;
using Azure;
using Indice.Extensions;
using Xunit;

namespace Indice.Services.Tests
{
    public class BlobPropertiesExtensionsTest
    {
        [Fact]
        public void EtagWithQuotes() {
            ETag sourceEtag = new ETag("\"0x8D979DAB44F6E71\"");
            var etag = sourceEtag.TryWrapEtagWithQuotes();
            Assert.NotNull(etag);
            Assert.StartsWith("\"", etag);
            Assert.EndsWith("\"", etag);
        }

        [Fact]
        public void EtagWithoutQuotes() {
            ETag sourceEtag = new ETag("0x8D979DAB44F6E71");
            var etag = sourceEtag.TryWrapEtagWithQuotes();
            Assert.NotNull(etag);
            Assert.StartsWith("\"", etag);
            Assert.EndsWith("\"", etag);
        }
    }
}
