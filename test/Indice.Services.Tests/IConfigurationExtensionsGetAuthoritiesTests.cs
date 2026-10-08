using Microsoft.Extensions.Configuration;
using Xunit;

namespace Indice.Services.Tests;

public class IConfigurationExtensionsGetAuthoritiesTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void GetAuthorities_WhenNotConfigured_ReturnsEmpty() {
        var config = BuildConfiguration([]);

        var result = config.GetAuthorities();

        Assert.Empty(result);
    }

    [Fact]
    public void GetAuthorities_WhenScalarValue_ReturnsSingleEntry() {
        var config = BuildConfiguration(new() {
            ["General:Authority"] = "https://idp.example.com"
        });

        var result = config.GetAuthorities();

        Assert.Single(result);
        Assert.Equal("https://idp.example.com", result.First());
    }

    [Fact]
    public void GetAuthorities_WhenScalarValueWithTrailingSlash_TrimsTrailingSlash() {
        var config = BuildConfiguration(new() {
            ["General:Authority"] = "https://idp.example.com/"
        });

        var result = config.GetAuthorities();

        Assert.Single(result);
        Assert.Equal("https://idp.example.com", result.First());
    }

    [Fact]
    public void GetAuthorities_WhenCommaSeparatedValue_ReturnsAllEntries() {
        var config = BuildConfiguration(new() {
            ["General:Authority"] = "https://idp1.example.com,https://idp2.example.com"
        });

        var result = config.GetAuthorities();

        Assert.Equal(2, result.Count());
        Assert.Contains("https://idp1.example.com", result);
        Assert.Contains("https://idp2.example.com", result);
    }

    [Fact]
    public void GetAuthorities_WhenCommaSeparatedValueWithSpaces_TrimsEntries() {
        var config = BuildConfiguration(new() {
            ["General:Authority"] = "https://idp1.example.com , https://idp2.example.com"
        });

        var result = config.GetAuthorities();

        Assert.Equal(2, result.Count());
        Assert.Contains("https://idp1.example.com", result);
        Assert.Contains("https://idp2.example.com", result);
    }

    [Fact]
    public void GetAuthorities_WhenCommaSeparatedValueWithTrailingSlashes_TrimsAllTrailingSlashes() {
        var config = BuildConfiguration(new() {
            ["General:Authority"] = "https://idp1.example.com/,https://idp2.example.com/"
        });

        var result = config.GetAuthorities();

        Assert.Equal(2, result.Count());
        Assert.All(result, url => Assert.False(url.EndsWith("/")));
    }

    [Fact]
    public void GetAuthorities_WhenArrayValue_ReturnsAllEntries() {
        var config = BuildConfiguration(new() {
            ["General:Authority:0"] = "https://idp1.example.com",
            ["General:Authority:1"] = "https://idp2.example.com"
        });

        var result = config.GetAuthorities();

        Assert.Equal(2, result.Count());
        Assert.Contains("https://idp1.example.com", result);
        Assert.Contains("https://idp2.example.com", result);
    }

    [Fact]
    public void GetAuthorities_WhenArrayValueWithTrailingSlashes_TrimsAllTrailingSlashes() {
        var config = BuildConfiguration(new() {
            ["General:Authority:0"] = "https://idp1.example.com/",
            ["General:Authority:1"] = "https://idp2.example.com/"
        });

        var result = config.GetAuthorities();

        Assert.Equal(2, result.Count());
        Assert.All(result, url => Assert.False(url.EndsWith("/")));
    }

    [Fact]
    public void GetAuthorities_WhenArrayValueWithTrailingSlashes_TrimsAllTrailingSlashes2() {
        var config = BuildConfiguration(new() {
            ["General:Authority"] = "https://idp1.example.com/,https://idp2.example.com/; https://idp2.example.com/",
            ["General:Authority:0"] = "https://idp1.example.com/,https://idp2.example.com/",
            ["General:Authority:1"] = "https://idp2.example.com/"
        });

        var result = config.GetAuthorities().Distinct();

        Assert.Equal(2, result.Count());
        Assert.All(result, url => Assert.False(url.EndsWith("/")));
    }

    [Fact]
    public void GetAuthority_WhenScalarValue_ReturnsSingleEntry() {
        var config = BuildConfiguration(new() {
            ["General:Authority"] = "https://idp.example.com"
        });

        var result = config.GetAuthority();

        Assert.Equal("https://idp.example.com", result);
    }

    [Fact]
    public void GetAuthority_WhenCommaSeparatedValue_ReturnsFirstEntry() {
        var config = BuildConfiguration(new() {
            ["General:Authority"] = "https://idp1.example.com,https://idp2.example.com"
        });

        var result = config.GetAuthority();

        Assert.Equal("https://idp1.example.com", result);
    }

    [Fact]
    public void GetAuthority_WhenArrayValue_ReturnsFirstEntry() {
        var config = BuildConfiguration(new() {
            ["General:Authority:0"] = "https://idp1.example.com",
            ["General:Authority:1"] = "https://idp2.example.com"
        });

        var result = config.GetAuthority();

        Assert.Equal("https://idp1.example.com", result);
    }


    [Fact]
    public void GetAuthorities_WhenConfiguration_DoesNotContainAuthority_ReturnsEmptyArray() {
        var config = BuildConfiguration(new() {});

        var result = config.GetAuthorities();

        Assert.Empty(result);
    }
}
