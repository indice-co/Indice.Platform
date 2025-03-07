using Xunit;
using Indice.Extensions;

namespace Indice.Common.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("BlobCreated", "blob-created")]
    [InlineData("Blob1Created02", "blob1-created02")]
    [InlineData("Blob1Crea02ted", "blob1-crea02ted")]
    [InlineData("1280x1024_audi_tt.jpg", "1280x1024-audi-tt.jpg")]
    [InlineData("1280 x 1024_ audi_ tt.jpg", "1280-x-1024-audi-tt.jpg")]
    public void KebabCaseTest(string input, string output) {
        Assert.Equal(input.ToKebabCase(), output);
    }
}
