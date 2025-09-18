using System.Globalization;
using Indice.Extensions;
using Xunit;

namespace Indice.Common.Tests;

public class FileExtensionsTests
{
    const long OneKiloByte = 1024L;
    const long OneMegaByte = OneKiloByte * 1024L;
    const long OneGigaByte = OneMegaByte * 1024L;
    const long OneTeraByte = OneGigaByte * 1024L;
    const long OnePetaByte = OneTeraByte * 1024L;
    const long OneExaByte = OnePetaByte * 1024L;

    [Theory]
    
    // bytes (B)
    [InlineData(0, "0 bytes")]
    [InlineData(1, "1 byte")]
    [InlineData(19, "19 bytes")]
    [InlineData(250, "250 bytes")]
    [InlineData(OneKiloByte - 1, "1,023 bytes")]

    // kilobytes (KB)
    [InlineData(OneKiloByte, "1.00 KB")]
    [InlineData(OneKiloByte + 16, "1.02 KB")]
    [InlineData(OneKiloByte * 10 + 205, "10.2 KB")]
    [InlineData(OneKiloByte * 110 + 512, "111 KB")]
    [InlineData(OneMegaByte - 1, "1,024 KB")]

    // megabytes (MB)
    [InlineData(OneMegaByte, "1.00 MB")]
    [InlineData(OneMegaByte + 16 * OneKiloByte, "1.02 MB")]
    [InlineData(OneMegaByte * 10 + 205 * OneKiloByte, "10.2 MB")]
    [InlineData(OneMegaByte * 110 + 512 * OneKiloByte, "111 MB")]
    [InlineData(OneGigaByte - 1, "1,024 MB")]

    // gigabytes (GB)
    [InlineData(OneGigaByte, "1.00 GB")]
    [InlineData(OneGigaByte + 16 * OneMegaByte, "1.02 GB")]
    [InlineData(OneGigaByte * 10 + 205 * OneMegaByte, "10.2 GB")]
    [InlineData(OneGigaByte * 110 + 512 * OneMegaByte, "111 GB")]
    [InlineData(OneTeraByte - OneMegaByte, "1,024 GB")]
    [InlineData(OneTeraByte - 1, "1,024 GB")]

    // terabytes (TB)
    [InlineData(OneTeraByte, "1.00 TB")]
    [InlineData(OneTeraByte + 16 * OneGigaByte, "1.02 TB")]
    [InlineData(OneTeraByte * 10 + 205 * OneGigaByte, "10.2 TB")]
    [InlineData(OneTeraByte * 110 + 512 * OneGigaByte, "111 TB")]
    [InlineData(OnePetaByte - OneKiloByte, "1,024 TB")]

    // petabytes (PB)
    [InlineData(OnePetaByte, "1.00 PB")]
    [InlineData(OnePetaByte + 16 * OneTeraByte, "1.02 PB")]
    [InlineData(OnePetaByte * 10 + 205 * OneTeraByte, "10.2 PB")]
    [InlineData(OnePetaByte * 110 + 512 * OneTeraByte, "111 PB")]
    [InlineData(OneExaByte - OneMegaByte, "1,024 PB")]

    // exabytes (EB) and beyond, up to long.MaxValue
    [InlineData(OneExaByte, "1.00 EB")]
    [InlineData(OneExaByte + 0.03125 * OneExaByte, "1.03 EB")]
    [InlineData(long.MaxValue - 0.125 * OneExaByte, "7.88 EB")]
    [InlineData(long.MaxValue, "8.00 EB")]
    public void FormatByteSizeTest(long byteSize, string formattedByteSize) {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        var result = FileExtensions.FormatByteSize(byteSize);
        Assert.Equal(formattedByteSize, result);
    }
}
