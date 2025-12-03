using System.Net;
using Indice.Features.GeoIP.Extensions;

namespace Indice.Feature.GeoIP.Tests;

public class IPAddressExtensionsTests
{
    [Theory]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("10.128.64.32", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("172.20.10.5", true)]
    [InlineData("192.168.0.1", true)]
    [InlineData("192.168.255.255", true)]
    [InlineData("192.168.1.100", true)]
    public void IsPrivate_Should_Return_True_For_IPv4_Private_Addresses(string ipAddress, bool expected)
    {
        // Arrange
        var ip = IPAddress.Parse(ipAddress);

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("8.8.8.8", false)]
    [InlineData("1.1.1.1", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("203.0.113.1", false)]
    [InlineData("198.51.100.1", false)]
    [InlineData("172.15.255.255", false)]  // Just outside 172.16.0.0/12 range
    [InlineData("172.32.0.1", false)]      // Just outside 172.16.0.0/12 range
    [InlineData("11.0.0.1", false)]        // Just outside 10.0.0.0/8 range
    [InlineData("9.255.255.255", false)]   // Just outside 10.0.0.0/8 range
    [InlineData("192.167.255.255", false)] // Just outside 192.168.0.0/16 range
    [InlineData("192.169.0.1", false)]     // Just outside 192.168.0.0/16 range
    [InlineData("0.0.0.0", false)]         // Network address
    [InlineData("255.255.255.255", false)] // Broadcast address
    public void IsPrivate_Should_Return_False_For_IPv4_Public_Addresses(string ipAddress, bool expected)
    {
        // Arrange
        var ip = IPAddress.Parse(ipAddress);

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("fe80::1", true)]          // Link-local
    [InlineData("fe80::abcd:ef01:2345", true)]  // Link-local
    [InlineData("fec0::1", true)]          // Site-local (deprecated but still considered private)
    [InlineData("fec0::abcd:ef01:2345", true)]  // Site-local
    public void IsPrivate_Should_Return_True_For_IPv6_Private_Addresses(string ipAddress, bool expected)
    {
        // Arrange
        var ip = IPAddress.Parse(ipAddress);

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2001:db8::1", false)]     // Documentation range
    [InlineData("2607:f8b0:4004:c1b::65", false)]  // Google DNS
    [InlineData("2001:4860:4860::8888", false)]    // Google DNS
    [InlineData("::1", false)]             // IPv6 loopback
    [InlineData("::", false)]              // IPv6 any address
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334", false)]  // Global unicast
    public void IsPrivate_Should_Return_False_For_IPv6_Public_Addresses(string ipAddress, bool expected)
    {
        // Arrange
        var ip = IPAddress.Parse(ipAddress);

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPrivate_Should_Handle_IPv4_Mapped_IPv6_Addresses()
    {
        // Arrange - IPv4-mapped IPv6 address for 192.168.1.1
        var ip = IPAddress.Parse("::ffff:192.168.1.1");

        // Act
        var result = ip.IsPrivate();

        // Assert
        // IPv4-mapped IPv6 addresses should be handled according to IPv6 rules
        // Since this is not link-local or site-local, it should return false
        Assert.False(result);
    }

    [Theory]
    [InlineData("10.0.0.0")]     // Network address
    [InlineData("10.255.255.255")] // Broadcast address for 10.0.0.0/8
    [InlineData("172.16.0.0")]   // Network address
    [InlineData("172.31.255.255")] // Broadcast address for 172.16.0.0/12
    [InlineData("192.168.0.0")]  // Network address
    [InlineData("192.168.255.255")] // Broadcast address for 192.168.0.0/16
    public void IsPrivate_Should_Return_True_For_Private_Network_And_Broadcast_Addresses(string ipAddress)
    {
        // Arrange
        var ip = IPAddress.Parse(ipAddress);

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("172.15.255.254")]  // One address before private range
    [InlineData("172.16.0.1")]      // First usable address in private range
    [InlineData("172.31.255.254")]  // Last usable address in private range
    [InlineData("172.32.0.1")]      // One address after private range
    public void IsPrivate_Should_Correctly_Handle_172_Range_Boundaries(string ipAddress)
    {
        // Arrange
        var ip = IPAddress.Parse(ipAddress);
        var expected = ipAddress.StartsWith("172.16.") || 
                      ipAddress.StartsWith("172.31.") ||
                      (ipAddress.StartsWith("172.") && 
                       int.Parse(ipAddress.Split('.')[1]) >= 16 && 
                       int.Parse(ipAddress.Split('.')[1]) <= 31);

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPrivate_Should_Work_With_IPAddress_Any()
    {
        // Arrange
        var ip = IPAddress.Any; // 0.0.0.0

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.False(result); // 0.0.0.0 is not a private address
    }

    [Fact]
    public void IsPrivate_Should_Work_With_IPAddress_Loopback()
    {
        // Arrange
        var ip = IPAddress.Loopback; // 127.0.0.1

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.False(result); // 127.0.0.1 is loopback, not private range
    }

    [Fact]
    public void IsPrivate_Should_Work_With_IPv6_Any()
    {
        // Arrange
        var ip = IPAddress.IPv6Any; // ::

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.False(result); // :: is not a private address
    }

    [Fact]
    public void IsPrivate_Should_Work_With_IPv6_Loopback()
    {
        // Arrange
        var ip = IPAddress.IPv6Loopback; // ::1

        // Act
        var result = ip.IsPrivate();

        // Assert
        Assert.False(result); // ::1 is loopback, not in private ranges
    }
}