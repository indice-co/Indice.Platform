using System.Reflection;

namespace Indice.EcmaScript.Tests;

/// <summary>
/// Responsible for ensuring the following generated assemblies have the expected JS/CSS assets embedded in them.
/// </summary>
public class EcmaScriptTests
{
    [Fact]
    public void Should_Have_Cases_Assets() {
        var assemblyName = "Indice.Features.Cases.UI.dll";
        var result = CheckForEmbeddedAssets(assemblyName);
        Assert.True(result);
    }

    [Fact]
    public void Should_Have_Messages_Assets() {
        var assemblyName = "Indice.Features.Messages.UI.dll";
        var result = CheckForEmbeddedAssets(assemblyName);
        Assert.True(result);
    }

    [Fact]
    public void Should_Have_Risk_Assets() {
        var assemblyName = "Indice.Features.Risk.UI.dll";
        var result = CheckForEmbeddedAssets(assemblyName);
        Assert.True(result);
    }

    [Fact]
    public void Should_Have_Identity_Admin_Assets() {
        var assemblyName = "Indice.Features.Identity.AdminUI.dll";
        var result = CheckForEmbeddedAssets(assemblyName);
        Assert.True(result);
    }

    private bool CheckForEmbeddedAssets(string assemblyFileName) {
        var assemblyPath = Path.Combine(Environment.CurrentDirectory, assemblyFileName);

        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath)) {
            return false;
        }

        var assembly = Assembly.LoadFrom(assemblyPath);
        var resourceNames = assembly.GetManifestResourceNames();

        return resourceNames.Any(x => !x.ToLowerInvariant().Contains("index.html"));
    }
}