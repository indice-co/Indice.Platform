using Indice.Features.Agents.Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers routable agents into the Agents catalogue consumed by the chat client, the intent router and the discovery endpoint.</summary>
public static class AgentsRegistrationExtensions
{
    /// <summary>
    /// Registers a ready-made agent. <see cref="AgentDefinition.CreateAgent"/> must be set. The definition becomes resolvable as
    /// <c>IEnumerable&lt;AgentDefinition&gt;</c>; agent names are unique (case-insensitive).
    /// </summary>
    /// <exception cref="ArgumentException">The definition has no name or no <see cref="AgentDefinition.CreateAgent"/> factory.</exception>
    /// <exception cref="InvalidOperationException">An agent with the same name is already registered.</exception>
    public static IServiceCollection AddAgent(this IServiceCollection services, AgentDefinition definition) {
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.Name)) {
            throw new ArgumentException("An agent definition must have a name.", nameof(definition));
        }
        if (definition.CreateAgent is null) {
            throw new ArgumentException($"Agent '{definition.Name}' has no CreateAgent factory. Use AddAgentWorkflow for workflow-backed agents.", nameof(definition));
        }
        var duplicate = services.Any(descriptor =>
            descriptor.ServiceType == typeof(AgentDefinition) &&
            descriptor.ImplementationInstance is AgentDefinition existing &&
            string.Equals(existing.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
        if (duplicate) {
            throw new InvalidOperationException($"An agent named '{definition.Name}' is already registered.");
        }
        services.AddSingleton(definition);
        return services;
    }

    /// <summary>
    /// Registers a MAF <see cref="Workflow"/> as a routable agent. The workflow is registered as a keyed scoped <see cref="Workflow"/>
    /// under the agent name and wrapped with <c>AsAIAgent</c> per request, so its start executor must accept chat messages
    /// (see <c>KnowledgeEntry</c> for the pipeline entry point pattern).
    /// </summary>
    public static IServiceCollection AddAgentWorkflow(this IServiceCollection services, AgentDefinition definition, Func<IServiceProvider, Workflow> workflowFactory) {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(workflowFactory);
        var name = definition.Name;
        services.AddKeyedScoped(name, (serviceProvider, _) => workflowFactory(serviceProvider));
        definition.CreateAgent = serviceProvider => serviceProvider
            .GetRequiredKeyedService<Workflow>(name)
            .AsAIAgent(id: name, name: name, description: definition.Description, includeExceptionDetails: true);
        return services.AddAgent(definition);
    }
}
