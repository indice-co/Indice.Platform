using Indice.Features.Agents.Core.Workflows.Routing;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.Core.Tests.Routing;

public class AgentsRegistrationTests
{
    private static AgentDefinition Definition(string name, bool isRouter = false) => new() {
        Name = name,
        Description = $"{name} agent",
        IsRouter = isRouter,
        CreateAgent = _ => throw new NotSupportedException("Not materialised in this test."),
    };

    [Fact]
    public void AddAgent_exposes_the_definition_through_the_catalogue() {
        var services = new ServiceCollection();
        services.AddAgent(Definition("billing"));
        services.AddAgent(Definition("auto", isRouter: true));

        using var provider = services.BuildServiceProvider();
        var names = provider.GetServices<AgentDefinition>().Select(definition => definition.Name).ToList();

        Assert.Equal(["billing", "auto"], names);
    }

    [Fact]
    public void AddAgent_rejects_duplicate_names_regardless_of_case() {
        var services = new ServiceCollection();
        services.AddAgent(Definition("billing"));

        Assert.Throws<InvalidOperationException>(() => services.AddAgent(Definition("BILLING")));
    }

    [Fact]
    public void AddAgent_requires_a_factory() {
        var services = new ServiceCollection();
        var definition = new AgentDefinition { Name = "billing", Description = "no factory" };

        Assert.Throws<ArgumentException>(() => services.AddAgent(definition));
    }

    [Fact]
    public void AddAgentWorkflow_registers_the_keyed_workflow_and_wires_the_agent_factory() {
        var services = new ServiceCollection();
        var definition = new AgentDefinition { Name = "echo", Description = "echoes" };

        services.AddAgentWorkflow(definition, _ => throw new NotSupportedException("Not built in this test."));

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(Workflow) && Equals(descriptor.ServiceKey, "echo") && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(AgentDefinition) && ReferenceEquals(descriptor.ImplementationInstance, definition));
        Assert.NotNull(definition.CreateAgent);
    }

    [Fact]
    public void Router_factory_refuses_to_build_without_targets() {
        using var empty = new ServiceCollection().BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => IntentRouterFactory.CreateHandoffWorkflow(empty));

        var services = new ServiceCollection();
        services.AddAgent(Definition("auto", isRouter: true));
        using var routerOnly = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => IntentRouterFactory.CreateHandoffWorkflow(routerOnly));
    }
}
