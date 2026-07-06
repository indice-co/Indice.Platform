using Indice.Extensions;
using Xunit;

namespace Indice.Common.Tests;

public class DateTimeExtensionsTests
{


    [Trait("Tag", "TimeZone")]
    [Fact]
    public void ConvertTimeZoneFromUtc() {
        var testDateTimeOffset = DateTimeOffset.Parse("2026-07-06T11:31:22.7300379+00:00");
        var dateTimeToTargetOffset = testDateTimeOffset.Convert("Europe/Athens");
        Assert.Equal("2026-07-06T14:31:22.7300379+03:00", dateTimeToTargetOffset.ToString("o"));
    }
}
