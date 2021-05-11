using Xunit;
using Indice.Extensions;

namespace Indice.Common.Tests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("BlobCreated", "blob-created")]
        [InlineData("Blob1Created02", "blob1-created02")]
        [InlineData("Blob1Crea02ted", "blob1-crea02ted")]
        public void KebabCaseTest(string input, string output) {
            Assert.Equal(input.ToKebabCase(), output);
        }
    }
}
