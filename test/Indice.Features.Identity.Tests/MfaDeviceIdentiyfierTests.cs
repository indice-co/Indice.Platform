using Indice.Features.Identity.Core.Models;
using Xunit;

namespace Indice.Features.Identity.Tests;
public class MfaDeviceIdentiyfierTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MfaDeviceIdentifier_Empty(string? deviceIdText) {
        var deviceId = new MfaDeviceIdentifier(deviceIdText);
        Assert.True(deviceId.IsEmpty);
        Assert.False(deviceId.HasRegistrationId);
        Assert.Equal(MfaDeviceIdentifier.Empty, deviceId);
    }

    [Theory]
    [InlineData("f4b0c1a2-3d4e-5f6a-7b8c-9d0e1f2a3b4c")]
    [InlineData("f4b0c1a23d4e5f6a7b8c9d0e1f2a3b4c")]
    [InlineData("F4B0C1A23D4E5F6A7B8C9D0E1F2A3B4C")]
    public void MfaDeviceIdentifier_ValidGuid(string guidText) {
        Assert.True(MfaDeviceIdentifier.ValidateDeviceId(guidText));
        var deviceId = new MfaDeviceIdentifier(guidText);
        Assert.False(deviceId.IsEmpty);
    }

    [Theory]
    [InlineData("2916aff63921d0f8d242619473bd60d5.Edge")]
    [InlineData("a5012912ea952907e1ee9093a52e33e8.Chrome")]
    [InlineData("A5012912EA952907E1EE9093A52E33E8")]
    public void MfaDeviceIdentifier_ValidFormat(string text) {
        Assert.True(MfaDeviceIdentifier.ValidateDeviceId(text));
        var deviceId = new MfaDeviceIdentifier(text);
        Assert.False(deviceId.IsEmpty);
    }

    [Theory]
    [InlineData("2916aff63921d0aSDAf8d242619473bd60d5.Edge")]
    [InlineData("a5012912ea952907e1ee9093a52eadsasdas33e8&Chrome")]
    [InlineData("A5012912EA952907E1EE--9093A52E33E8")]
    public void MfaDeviceIdentifier_InvalidValidFormat(string guidText) {
        Assert.False(MfaDeviceIdentifier.ValidateDeviceId(guidText));
    }
}
