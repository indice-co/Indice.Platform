using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Agents.Core.Tests;

/// <summary>The hooks an agent registered outside this package uses: metadata built from services, prompt defaults and step labels.</summary>
public class AgentExtensionHookTests
{
    [Fact]
    public void RegisteredPromptDefault_IsRendered() {
        var renderer = new FileSystemPromptTemplateRenderer(new TestHostEnvironment(EmptyContentRoot()), [new PromptTemplateDefault("External", "Hello {{name}}")]);

        Assert.Equal("Hello Dex", renderer.Render("External", new { name = "Dex" }));
    }

    [Fact]
    public void PromptFile_OverridesRegisteredDefault() {
        var contentRoot = EmptyContentRoot();
        Directory.CreateDirectory(Path.Join(contentRoot, "Prompts"));
        File.WriteAllText(Path.Join(contentRoot, "Prompts", "External.txt"), "Overridden {{name}}");
        var renderer = new FileSystemPromptTemplateRenderer(new TestHostEnvironment(contentRoot), [new PromptTemplateDefault("External", "Hello {{name}}")]);

        Assert.Equal("Overridden Dex", renderer.Render("External", new { name = "Dex" }));
    }

    [Fact]
    public void LastRegisteredPromptDefault_Wins() {
        var renderer = new FileSystemPromptTemplateRenderer(new TestHostEnvironment(EmptyContentRoot()), [new PromptTemplateDefault("External", "first"), new PromptTemplateDefault("External", "second")]);

        Assert.Equal("second", renderer.Render("External"));
    }

    [Fact]
    public void UnknownPrompt_StillThrows() {
        var renderer = new FileSystemPromptTemplateRenderer(new TestHostEnvironment(EmptyContentRoot()), []);

        Assert.Throws<InvalidOperationException>(() => renderer.Render("Missing"));
    }

    [Fact]
    public void AddPromptTemplateDefault_ReachesTheRenderer() {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(EmptyContentRoot()));
        services.AddSingleton<IPromptTemplateRenderer, FileSystemPromptTemplateRenderer>();
        services.AddPromptTemplateDefault("External", "From DI");

        using var provider = services.BuildServiceProvider();

        Assert.Equal("From DI", provider.GetRequiredService<IPromptTemplateRenderer>().Render("External"));
    }

    [Fact]
    public void AddRoutableAgent_WithFactory_IsDiscoverable() {
        var services = new ServiceCollection();
        services.AddSingleton<AgentInfoRegistry>();
        services.AddRoutableAgent("external", _ => Info("external"), (_, _) => throw new NotSupportedException());

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<AgentInfoRegistry>();

        Assert.NotNull(registry.Find("external"));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(Workflow) && Equals(descriptor.ServiceKey, "external"));
    }

    [Fact]
    public void AddRoutableAgent_WithFactory_UsesServices() {
        var services = new ServiceCollection();
        services.AddSingleton(new RolesHolder(["Auditor"]));
        services.AddSingleton<AgentInfoRegistry>();
        services.AddRoutableAgent("external", sp => Info("external") with { RequiredRoles = sp.GetRequiredService<RolesHolder>().Roles }, (_, _) => throw new NotSupportedException());

        using var provider = services.BuildServiceProvider();

        Assert.Equal(["Auditor"], provider.GetRequiredService<AgentInfoRegistry>().Find("external")!.RequiredRoles!);
    }

    [Fact]
    public void AddRoutableAgent_WithMismatchedName_Throws() {
        var services = new ServiceCollection();
        services.AddSingleton<AgentInfoRegistry>();
        services.AddRoutableAgent("external", _ => Info("someone-else"), (_, _) => throw new NotSupportedException());

        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<AgentInfoRegistry>());
    }

    [Fact]
    public void AddAgentStepLabel_RegistersTheLabel() {
        var services = new ServiceCollection();
        services.AddAgentStepLabel("ExternalStep", _ => "Working on it");

        using var provider = services.BuildServiceProvider();
        var label = Assert.Single(provider.GetServices<AgentStepLabel>());

        Assert.Equal("ExternalStep", label.ExecutorId);
        Assert.Equal("Working on it", label.Label(provider));
    }

    private static AgentInfo Info(string name) => new(name, $"{name} agent", ["text/plain"], ["text/markdown"]);

    private static string EmptyContentRoot() => Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    private sealed record RolesHolder(IReadOnlyList<string> Roles);

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
