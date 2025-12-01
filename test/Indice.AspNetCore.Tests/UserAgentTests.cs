using Indice.AspNetCore;
using Indice.Types;
using Xunit;

namespace Indice.AspNetCore.Tests;

public class UserAgentTests
{
    [Theory]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Chrome 120.0.0 on Windows 10",
        "Windows 10",
        DevicePlatform.Windows,
        null)]
    [InlineData(
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
        "Mobile Safari 17.1 on iOS 17.1.2",
        "iOS 17.1.2",
        DevicePlatform.iOS,
        "Apple iPhone")]
    [InlineData(
        "Mozilla/5.0 (Linux; Android 14; Pixel 7 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
        "Chrome Mobile 120.0.0 on Android 14",
        "Android 14",
        DevicePlatform.Android,
        "Google Pixel 7 Pro")]
    [InlineData(
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1.2 Safari/605.1.15",
        "Safari 17.1.2 on Mac OS X 10.15.7",
        "Mac OS X 10.15.7",
        DevicePlatform.MacOS,
        "Apple Mac")]
    [InlineData(
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/121.0",
        "Firefox 121.0 on Ubuntu",
        "Ubuntu",
        DevicePlatform.Linux,
        null)]
    [InlineData(
        "Mozilla/5.0 (iPad; CPU OS 17_1_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1.2 Mobile/15E148 Safari/604.1",
        "Mobile Safari 17.1.2 on iOS 17.1.2",
        "iOS 17.1.2",
        DevicePlatform.iOS,
        "Apple iPad")]
    [InlineData(
        "Mozilla/5.0 (Linux; Android 13; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
        "Chrome Mobile 120.0.0 on Android 13",
        "Android 13",
        DevicePlatform.Android,
        "Samsung SM-S918B")]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Firefox 121.0 on Windows 10",
        "Windows 10",
        DevicePlatform.Windows,
        null)]
    [InlineData(
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Chrome 120.0.0 on Mac OS X 10.15.7",
        "Mac OS X 10.15.7",
        DevicePlatform.MacOS,
        "Apple Mac")]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0",
        "Edge 120.0.0 on Windows 10",
        "Windows 10",
        DevicePlatform.Windows,
        null)]
    [InlineData(
        "Mozilla/5.0 (Linux; Android 12; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Mobile Safari/537.36",
        "Chrome Mobile 119.0.0 on Android 12",
        "Android 12",
        DevicePlatform.Android,
        "Samsung SM-G991B")]
    [InlineData(
        "PostmanRuntime/7.35.0",
        "PostmanRuntime 7.35.0 on Other",
        "Other",
        DevicePlatform.None,
        null)]
    [InlineData(
        "",
        "Other on Other",
        "Other",
        DevicePlatform.None,
        null)]
    public void UserAgent_Should_Parse_Correctly(
        string userAgentString,
        string expectedDisplayName,
        string? expectedOs,
        DevicePlatform expectedPlatform,
        string? expectedDeviceModel)
    {
        // Act
        var userAgent = new UserAgent(userAgentString);

        // Assert
        Assert.Equal(expectedDisplayName, userAgent.DisplayName);
        Assert.Equal(expectedOs, userAgent.Os);
        Assert.Equal(expectedPlatform, userAgent.DevicePlatform);
        Assert.Equal(expectedDeviceModel, userAgent.DeviceModel);
        Assert.Equal(userAgentString, userAgent.HeaderValue);
    }

    [Fact]
    public void UserAgent_Should_Throw_ArgumentNullException_For_Null_UserAgent()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UserAgent(null!));
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")]
    [InlineData("Chrome/120.0.0.0")]
    [InlineData("Safari/537.36")]
    [InlineData("CustomBot/1.0")]
    public void UserAgent_Should_Handle_Incomplete_UserAgent_Strings(string userAgentString)
    {
        // Act
        var userAgent = new UserAgent(userAgentString);

        // Assert
        Assert.NotNull(userAgent.DisplayName);
        Assert.NotNull(userAgent.HeaderValue);
        Assert.Equal(userAgentString, userAgent.HeaderValue);
        // Should not throw exceptions for incomplete user agent strings
        Assert.True(Enum.IsDefined(typeof(DevicePlatform), userAgent.DevicePlatform));
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X)", DevicePlatform.iOS)]
    [InlineData("Mozilla/5.0 (Linux; Android 11; Pixel 5)", DevicePlatform.Android)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", DevicePlatform.Windows)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)", DevicePlatform.MacOS)]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64)", DevicePlatform.Linux)]
    [InlineData("SomeBot/1.0", DevicePlatform.None)]
    public void UserAgent_Should_Detect_Platform_Correctly(string userAgentString, DevicePlatform expectedPlatform)
    {
        // Act
        var userAgent = new UserAgent(userAgentString);

        // Assert
        Assert.Equal(expectedPlatform, userAgent.DevicePlatform);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_1_2 like Mac OS X) AppleWebKit/605.1.15", "Apple")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 7 Pro) AppleWebKit/537.36", "Google")]
    [InlineData("Mozilla/5.0 (Linux; Android 13; SM-S918B) AppleWebKit/537.36", "Samsung")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", null)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15", "Apple Mac")]
    public void UserAgent_Should_Extract_Device_Brand_When_Present(string userAgentString, string? expectedBrand)
    {
        // Act
        var userAgent = new UserAgent(userAgentString);

        // Assert
        if (expectedBrand != null)
        {
            Assert.Contains(expectedBrand, userAgent.DeviceModel ?? "");
        }
        else
        {
            // For desktop platforms, device model is typically null or empty
            Assert.True(string.IsNullOrEmpty(userAgent.DeviceModel));
        }
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", "Chrome")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_1_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1", "Mobile Safari")]
    [InlineData("Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/121.0", "Firefox")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0", "Edge")]
    public void UserAgent_DisplayName_Should_Include_Browser_Name(string userAgentString, string expectedBrowser)
    {
        // Act
        var userAgent = new UserAgent(userAgentString);

        // Assert
        Assert.Contains(expectedBrowser, userAgent.DisplayName);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 6.1; Win64; x64)", "Windows 7")]
    [InlineData("Mozilla/5.0 (Windows NT 6.3; Win64; x64)", "Windows 8.1")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", "Windows 10")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 16_7_2 like Mac OS X)", "iOS 16.7.2")]
    [InlineData("Mozilla/5.0 (Linux; Android 13; Pixel 7)", "Android 13")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)", "Mac OS X 10.15.7")]
    public void UserAgent_Should_Parse_OS_Version_Correctly(string userAgentString, string expectedOs)
    {
        // Act
        var userAgent = new UserAgent(userAgentString);

        // Assert
        Assert.Equal(expectedOs, userAgent.Os);
    }

    [Fact]
    public void UserAgent_Properties_Should_Be_Consistent_With_UAParser_Results()
    {
        // Arrange
        var userAgentString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        // Act
        var userAgent = new UserAgent(userAgentString);

        // Assert - Verify that all properties are properly initialized
        Assert.NotNull(userAgent.DisplayName);
        Assert.NotNull(userAgent.HeaderValue);
        Assert.Equal(userAgentString, userAgent.HeaderValue);
        Assert.True(userAgent.DevicePlatform != DevicePlatform.None);
        // OS should be parsed for common user agents
        Assert.NotNull(userAgent.Os);
        Assert.Contains("Windows", userAgent.Os);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
    [InlineData("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)")]
    [InlineData("Twitterbot/1.0")]
    [InlineData("WhatsApp/2.23.20.0")]
    public void UserAgent_Should_Handle_Bot_And_Crawler_UserAgents(string userAgentString)
    {
        // Act
        var userAgent = new UserAgent(userAgentString);

        // Assert
        Assert.Equal(userAgentString, userAgent.HeaderValue);
        Assert.NotNull(userAgent.DisplayName);
        // Bots typically don't have a specific platform
        Assert.Equal(DevicePlatform.None, userAgent.DevicePlatform);
    }
}