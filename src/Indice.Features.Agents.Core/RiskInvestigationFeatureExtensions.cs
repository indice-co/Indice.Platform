using Duende.AccessTokenManagement;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services.Risk;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Steps.Operator;
using Indice.Features.Agents.Core.Workflows.Steps.Risk;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions that register the risk investigation agent.</summary>
public static class RiskInvestigationFeatureExtensions
{
    /// <summary>The name of the client-credentials client the MCP servers are called with.</summary>
    internal const string McpSecurityClient = "mcpsecurity";

    /// <summary>
    /// Registers the machine-to-machine token client every MCP client of the agents host authenticates with. Calling it more than once
    /// (the operator and the risk agent both do) just repeats the same configuration.
    /// </summary>
    internal static IServiceCollection AddMcpSecurityTokenClient(this IServiceCollection services, IConfiguration configuration) {
        services.AddClientCredentialsTokenManagement()
                .AddClient(McpSecurityClient, credentials => {
                    // Machine-to-machine authentication (no user present, no redirect/browser).
                    credentials.TokenEndpoint = new Uri(configuration["General:Endpoints:TokenEndpoint"]!);
                    credentials.ClientId = ClientId.Parse(configuration["General:Secrets:ClientId"]!);
                    credentials.ClientSecret = ClientSecret.Parse(configuration["General:Secrets:ClientSecret"]!);
                    credentials.Scope = Scope.Parse(configuration["General:Secrets:Scope"]!);
                });
        return services;
    }

    /// <summary>
    /// Registers the risk investigation agent: a workflow that takes a transaction number and a date, traces the transaction through the
    /// risk MCP server, and produces an evidence report for legal use (see the <c>risk-kb</c> playbook P-02). Call after
    /// <c>AddAgentsCore(...)</c>.
    /// </summary>
    /// <remarks>
    /// The agent is never open to everyone. <c>Dex:Risk:RequiredRoles</c> must list at least one role, or the host does not start.
    /// <c>General:Endpoints:RiskMCP</c> is the URL of the risk MCP server; the token client shares the <c>General:Endpoints:TokenEndpoint</c>
    /// and <c>General:Secrets</c> settings with the other MCP clients, and its scope must include <c>risk-mcp</c>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureOptions">An optional delegate to configure <see cref="RiskInvestigationOptions"/> after binding.</param>
    public static IServiceCollection AddRiskInvestigationWorkflow(this IServiceCollection services, IConfiguration configuration, Action<RiskInvestigationOptions>? configureOptions = null) {
        var riskMcp = configuration["General:Endpoints:RiskMCP"];
        if (string.IsNullOrWhiteSpace(riskMcp)) {
            throw new InvalidOperationException("General:Endpoints:RiskMCP must be set to the URL of the risk MCP server to use the risk investigation agent.");
        }

        services.AddOptions<RiskInvestigationOptions>()
                .Bind(configuration.GetSection(RiskInvestigationOptions.SectionName))
                .Configure(options => configureOptions?.Invoke(options))
                .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<RiskInvestigationOptions>, RiskInvestigationOptionsValidator>());
        services.TryAddSingleton(TimeProvider.System);

        // The role list comes from options, so the metadata is built when first needed. If the list is somehow empty the agent must
        // not appear as open to everyone: it fails instead.
        services.AddSingleton(sp => {
            var options = sp.GetRequiredService<IOptions<RiskInvestigationOptions>>().Value;
            if (options.RequiredRoles.Count == 0) {
                throw new InvalidOperationException($"{RiskInvestigationOptions.SectionName}:RequiredRoles must list at least one role.");
            }
            return new AgentInfo(
                Name: AgentsConstants.AgentNames.Risk,
                Description: "Traces a banking transaction by its transaction number (TUN) through the risk, sign-in, activity and identity logs and produces an evidence report for legal use: court filings, prosecutor requests and customer disputes.",
                InputContentTypes: ["text/plain"],
                OutputContentTypes: ["text/markdown", AgentsConstants.MediaTypes.File, AgentsConstants.MediaTypes.FunctionCallPort.Request],
                Capabilities: [
                    new AgentCapability("Transaction trace", "Reconstructs what happened around a transaction from its TUN: session, login, authentication steps, one-time passwords, device and network."),
                    new AgentCapability("Evidence report", "Builds a report with sources and limitations for a court, a prosecutor or a customer dispute (φάκελος τεκμηρίωσης, δικαστήριο, εισαγγελέας, αμφισβήτηση συναλλαγής).")],
                Domains: ["antifraud", "risk", "legal"],
                Tags: ["risk", "antifraud", "legal", "workflow"],
                Links: [],
                Icon: AgentsConstants.AgentIcons.Gear) {
                RequiredRoles = [.. options.RequiredRoles],
                KnowledgeCategories = [options.KnowledgeCategory]
            };
        });

        services.AddMcpSecurityTokenClient(configuration);
        services.AddMcpClient(RiskQueryClient.McpServiceKey)
                .WithClientCredentialsHttpTransport(new Uri(riskMcp), ClientCredentialsClientName.Parse(McpSecurityClient));

        services.TryAddTransient<IRiskQueryClient, RiskQueryClient>();
        services.TryAddTransient<RiskRequestAnalyzer>();
        services.TryAddTransient<RiskUnsupportedResponder>();
        services.TryAddTransient<TransactionAnchorResolver>();
        services.TryAddTransient<EvidenceCollector>();
        services.TryAddTransient<RiskReportComposer>();
        services.TryAddTransient<UserInputCollector>();

        services.AddKeyedScoped(AgentsConstants.AgentNames.Risk, (sp, key) => BuildWorkflow(sp));
        return services;
    }

    /// <summary>
    /// The risk investigation graph: <c>analyzer → anchor resolver → evidence collector → report composer</c>, with the analyzer and the
    /// resolver able to pause the run to ask the user for a missing detail, and the answer going back to the analyzer.
    /// </summary>
    internal static Workflow BuildWorkflow(IServiceProvider sp) {
        var analyzer = sp.GetRequiredService<RiskRequestAnalyzer>();
        var unsupported = sp.GetRequiredService<RiskUnsupportedResponder>();
        var resolver = sp.GetRequiredService<TransactionAnchorResolver>();
        var collector = sp.GetRequiredService<EvidenceCollector>();
        var composer = sp.GetRequiredService<RiskReportComposer>();
        var inputCollector = sp.GetRequiredService<UserInputCollector>();
        var inputPort = UserInputRequestPort.Create();

        var builder = new WorkflowBuilder(analyzer);
        // The switch in MAF routes on the predicate, which receives null when the message is of another type; see AddOperatorWorkflow.
        builder.AddSwitch(analyzer, sw => sw
            .AddCase<UserInputRequestPort.UserInputRequest>(message => message is not null, inputPort)
            .AddCase<RiskTraceRequest>(message => message is not null, resolver)
            .WithDefault(unsupported));
        // The user's answer to a question goes back to the analyzer, which merges it with what it already has.
        builder.AddEdge(inputPort, inputCollector);
        builder.AddEdge(inputCollector, analyzer);
        builder.AddSwitch(resolver, sw => sw
            .AddCase<RiskCase>(message => message is not null, collector)
            .WithDefault(inputPort));
        builder.AddEdge(collector, composer);
        builder.WithOutputFrom(composer, unsupported);
        return builder.Build();
    }
}
