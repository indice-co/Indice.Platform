using Indice.Extensions;
using Xunit;

namespace Indice.Common.Tests;

public class DateTimeExtensionsTests
{


    [Trait("Tag", "TimeZone")]
    [Theory]
    [InlineData("Europe/Athens", "2026-07-06T14:31:22.7300379+03:00")]
    [InlineData("America/New_York", "2026-07-06T07:31:22.7300379-04:00")]
    [InlineData("GTB Standard Time", "2026-07-06T14:31:22.7300379+03:00")]
    public void ConvertTimeZoneFromUtc(string timeZoneId, string expected) {
        var testDateTimeOffset = DateTimeOffset.Parse("2026-07-06T11:31:22.7300379+00:00");
        var dateTimeToTargetOffset = testDateTimeOffset.Convert(timeZoneId);
        Assert.Equal(expected, dateTimeToTargetOffset.ToString("o"));
    }
}
