using System.Reflection;
using Indice.Features.Risk.UI;
using Microsoft.AspNetCore.Builder;

namespace Indice.EcmaScript.Tests;

/// <summary>
/// Responsible for ensuring the following generated assemblies have the expected JS/CSS assets embedded in them.
/// </summary>
public class EcmaScriptTests
{
    [Fact]
    public void Should_Have_Cases_Assets() {
        var result = CheckForEmbeddedAssets(typeof(CasesUIMiddlewareExtensions).Assembly);
        Assert.True(result);
    }

    [Fact]
    public void Should_Have_Messages_Assets() {
        var result = CheckForEmbeddedAssets(typeof(CampaignsUIMiddlewareExtensions).Assembly);
        Assert.True(result);
    }

    [Fact]
    public void Should_Have_Risk_Assets() {
        var result = CheckForEmbeddedAssets(typeof(RisksUIMiddlewareExtensions).Assembly);
        Assert.True(result);
    }

    [Fact]
    public void Should_Have_Identity_Admin_Assets() {
        var result = CheckForEmbeddedAssets(typeof(AdminUIMiddlewareExtensions).Assembly);
        Assert.True(result);
    }

    private bool CheckForEmbeddedAssets(Assembly assembly) {
        var resourceNames = assembly.GetManifestResourceNames();

        return resourceNames.Any(x => !x.ToLowerInvariant().Contains("index.html"));
    }
}