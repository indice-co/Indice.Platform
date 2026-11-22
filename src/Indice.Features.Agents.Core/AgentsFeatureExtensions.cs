using System.ClientModel;
using Azure.AI.OpenAI;
using Duende.AccessTokenManagement;
using Google.Protobuf;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Providers;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Cards;
using Indice.Features.Agents.Core.Workflows.Demo;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.Reranking;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Features.Agents.Core.Workflows.Steps;
using Indice.Features.Agents.Core.Workflows.Steps.Operator;
using Microsoft.Agents.AI.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using static Indice.Features.Agents.Core.AgentsOptions;
using Indice.Features.Agents.Core.Services.Validation;
using Indice.Features.Agents.Core.Services.DataResolver;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions to configure the Agents feature.</summary>
public static class AgentsFeatureExtensions
{
    /// <summary>
    /// Registers Dex core services: <see cref="AgentsOptions"/> (bound from the <c>Dex</c> configuration section).
    /// </summary>
    /// <param name="services">The service collection to add the feature to.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureAction">An optional delegate to configure the <see cref="AgentsOptions"/>.</param>
    /// <param name="configureModelProviders">
    /// An optional delegate to plug in additional model providers (for example <c>providers.UseFreeLLM()</c>). Azure AI Foundry,
    /// configured from <see cref="AgentsOptions.AzureOpenAI"/>, serves every model role no provider claims.
    /// </param>
    public static IServiceCollection AddAgentsCore(this IServiceCollection services, IConfiguration configuration, Action<AgentsOptions>? configureAction = null, Action<AgentsModelProviderBuilder>? configureModelProviders = null) {
        var optionsBuilder = services.AddOptions<AgentsOptions>().BindConfiguration("Dex");
        if (configureAction is not null) {
            optionsBuilder.Configure(configureAction);
        }
        services.AddSingleton<IValidateOptions<AgentsOptions>, AgentsOptionsValidator>();
        optionsBuilder.ValidateOnStart();

        services.AddOptions<ModelsOptions>()
            .BindConfiguration("Dex:Models").Configure<IOptions<AgentsOptions>>((models, agents) => {
                agents.Value.ConfigureModelOptions?.Invoke(models);
            });

        services.AddSingleton(sp => {
            var opts = sp.GetRequiredService<IOptions<AgentsOptions>>().Value.AzureOpenAI;
            return new AzureOpenAIClient(new Uri(opts.Endpoint!), new ApiKeyCredential(opts.ApiKey!));
        });
        // The built-in default provider; it serves every role no provider registered through configureModelProviders claims.
        services.TryAddSingleton<AzureAIFoundryModelProvider>();
        services.AddKeyedChatClient(nameof(AgentsModelRole.Reasoning),
            sp => AgentsModelProviderResolver.Resolve(sp, AgentsModelRole.Reasoning).CreateChatClient(AgentsModelRole.Reasoning));
        services.AddKeyedChatClient(nameof(AgentsModelRole.Fast),
            sp => AgentsModelProviderResolver.Resolve(sp, AgentsModelRole.Fast).CreateChatClient(AgentsModelRole.Fast));
        services.AddEmbeddingGenerator(
            sp => AgentsModelProviderResolver.Resolve(sp, AgentsModelRole.Embedding).CreateEmbeddingGenerator());

        services.AddHttpClient(nameof(AzureOpenAIDeployments.SystemOne));
        services.TryAddTransient<ISystemOneClient>(sp => {
            var agentsOptions = sp.GetRequiredService<IOptions<AgentsOptions>>().Value;
            var opts = agentsOptions.AzureOpenAI;
            var enabled = agentsOptions.SystemOne.Enabled && !string.IsNullOrWhiteSpace(opts.Deployments.SystemOne);
            if (!enabled) {
                // Off by default. Needs no Azure settings, so hosts that serve every role from another provider can still route.
                return DisabledSystemOneClient.Instance;
            }
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AzureOpenAIDeployments.SystemOne));
            var endpoint = string.IsNullOrWhiteSpace(opts.SystemOneEndpoint) ? opts.Endpoint! : opts.SystemOneEndpoint;
            var apiKey = string.IsNullOrWhiteSpace(opts.SystemOneApiKey) ? opts.ApiKey! : opts.SystemOneApiKey;
            return new FoundrySystemOneClient(httpClient, new Uri(endpoint), apiKey, opts.Deployments.SystemOne!, enabled: true);
        });

        services.AddDbContext<AgentsDbContext>((sp, options) => {
            var opts = sp.GetRequiredService<IOptions<AgentsOptions>>().Value;
            var configureDbContext = opts.ConfigureDbContext ?? (
                (sp, dbContextOptions) => {
                    var connectionString = configuration.GetConnectionString("DexDb")
                        ?? throw new InvalidOperationException("Connection string 'DexDb' is not configured.");
                    dbContextOptions.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dex"));
                });
            configureDbContext.Invoke(sp, options);
        });

        services.TryAddTransient<UserClaimsAIContextProvider>();
        services.TryAddSingleton<AgentIdentityAIContextProvider>();
        services.TryAddTransient<IConversationStore, ConversationStore>();
        services.TryAddScoped<PersistedCheckpointStore>();
        services.TryAddScoped(sp => CheckpointManager.CreateJson(sp.GetRequiredService<PersistedCheckpointStore>()));
        services.TryAddTransient<IUsageGuardService, UsageGuardService>();
        services.TryAddTransient<ConversationStoreChatHistoryProvider>();
        services.TryAddSingleton<IPromptTemplateRenderer, FileSystemPromptTemplateRenderer>();
        services.TryAddTransient<IDexChatClient, AgentsChatClient>();
        services.TryAddSingleton<AgentsClaimsPrincipalSelector>(sp =>
            () => null
        );
        services.TryAddSingleton<ISourceLinkGenerator, NoOpSourceLinkGenerator>();
        services.TryAddSingleton<AgentInfoRegistry>();
        services.TryAddTransient<IKnowledgeHintService, KnowledgeHintService>();
        services.TryAddTransient<IConversationFileStore, ConversationFileStore>();
        services.TryAddTransient<IntentRouterService>();
        services.TryAddScoped<AgentMessageLocalizer>();
        services.AddAgentsDefaultPipeline();
        configureModelProviders?.Invoke(new AgentsModelProviderBuilder(services));
        return services;
    }

    /// <summary>
    /// Registers the five default steps, the default <see cref="ILlmReranker"/>, and a scoped
    /// <see cref="Workflow"/> wiring them in order. Call after <c>AddDex(...)</c>.
    /// </summary>
    public static IServiceCollection AddAgentsDefaultPipeline(this IServiceCollection services) {
        services.TryAddTransient<Indice.Features.Agents.Core.Workflows.Steps.IntentClassifier>();
        services.TryAddTransient<QueryRewriter>();
        services.TryAddTransient<Retriever>();
        services.TryAddTransient<Reranker>();
        services.TryAddTransient<AnswerComposer>();
        services.TryAddTransient<OutOfScopeResponder>();
        services.TryAddTransient<PurposeResponder>();
        services.TryAddTransient<ILlmReranker, LlmListwiseReranker>();

        // The meta "auto" router advertises itself for discovery but has no workflow of its own — it runs the
        // IntentRouterService to pick one of the routable agents registered below.
        services.AddSingleton(new AgentInfo(
            Name: AgentsConstants.AgentNames.Auto,
            Description: "Discovers the user's intent and routes the request to the appropriate sub-agent.",
            InputContentTypes: ["text/plain"],
            OutputContentTypes: ["text/markdown", AgentsConstants.MediaTypes.MultipleChoice, AgentsConstants.MediaTypes.Callout,
                                 AgentsConstants.MediaTypes.Image, AgentsConstants.MediaTypes.Confirmation, "image/png"],
            Capabilities: [new AgentCapability("Master intent classification", "Discovers user intent and routes it to the appropriate sub-agent.")],
            Domains: [],
            Tags: ["Intent"],
            Links: [],
            Icon: AgentsConstants.AgentIcons.Sparkles));

        // Register the knowledge workflow together with its metadata. Step failures are not handled here — a
        // throwing executor halts the run and AgentsChatClient reads the resulting error event.
        services.AddRoutableAgent(
            new AgentInfo(
                Name: AgentsConstants.AgentNames.Knowledge,
                Description: "Answers questions grounded in the configured knowledge base.",
                InputContentTypes: ["text/plain"],
                OutputContentTypes: ["text/markdown", AgentsConstants.MediaTypes.MultipleChoice, AgentsConstants.MediaTypes.Callout,
                                     AgentsConstants.MediaTypes.Image, AgentsConstants.MediaTypes.Confirmation, "image/png"],
                Capabilities: [new AgentCapability("Knowledge retrieval", "Answers questions based on a knowledge base.")],
                Domains: [],
                Tags: ["Knowledge", "FAQ"],
                Links: [],
                Icon: AgentsConstants.AgentIcons.Book),
            (sp, key) => {
                var intent = sp.GetRequiredService<Indice.Features.Agents.Core.Workflows.Steps.IntentClassifier>();
                var rewrite = sp.GetRequiredService<QueryRewriter>();
                var retrieve = sp.GetRequiredService<Retriever>();
                var rerank = sp.GetRequiredService<Reranker>();
                var compose = sp.GetRequiredService<AnswerComposer>();
                var outOfScopeReply = sp.GetRequiredService<OutOfScopeResponder>();
                var purposeResponder = sp.GetRequiredService<PurposeResponder>();

                var builder = new WorkflowBuilder(intent);
                builder.AddSwitch(intent, sw => sw
                    .AddCase<IntentOutput>(env => env!.Intent.Category == "purpose_of_agent", purposeResponder)
                    .AddCase<IntentOutput>(env => env!.Intent.IsInScope, rewrite)
                    .WithDefault(outOfScopeReply));
                builder.AddEdge(rewrite, retrieve);
                builder.AddEdge(retrieve, rerank);
                builder.AddEdge(rerank, compose);
                builder.WithOutputFrom(compose, outOfScopeReply, purposeResponder);
                return builder.Build();
            });

        return services;
    }

    /// <summary>
    /// Registers a demo workflow together with its metadata.
    /// </summary>
    /// <param name="services">The service collection to add the demo workflow to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAgentsDemoPipeline(this IServiceCollection services) {
        services.AddRoutableAgent(
            new AgentInfo(
                Name: AgentsConstants.AgentNames.Demo,
                Description: "Its the demo agent that mainly acts as a placeholder and tests capabilities.",
                InputContentTypes: ["text/plain"],
                OutputContentTypes: ["text/markdown"],
                Capabilities: [new AgentCapability("Demo Agent", "Its the demo agent for testing new workflow interactions.")],
                Domains: [],
                Tags: ["Demo", "SCA"],
                Links: [],
                Icon: AgentsConstants.AgentIcons.Gear),
            (sp, key) => {
                return DemoWorkflow.CreateDemoWorkflow(sp);
            });
        return services;
    }


    /// <summary>
    /// Registers a routable agent: a keyed <see cref="Workflow"/> resolved by <paramref name="info"/>'s name, plus
    /// its <see cref="AgentInfo"/> metadata. The metadata is collected by <see cref="AgentInfoRegistry"/> (and the
    /// discovery endpoint) through <c>IEnumerable&lt;AgentInfo&gt;</c>. Plain <c>AddSingleton</c> registers the
    /// metadata on purpose: every <see cref="AgentInfo"/> shares one implementation type, so <c>TryAddEnumerable</c>
    /// would dedupe them down to a single entry. Call after <c>AddAgentsCore(...)</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="info">The agent metadata.</param>
    /// <param name="workflowFactory">Builds the agent's workflow.</param>
    public static IServiceCollection AddRoutableAgent(this IServiceCollection services, AgentInfo info, Func<IServiceProvider, object?, Workflow> workflowFactory) {
        services.AddKeyedScoped(info.Name, workflowFactory);
        services.AddSingleton(info);
        return services;
    }

    /// <summary>
    /// Registers a routable agent whose metadata depends on other services, for example an <see cref="AgentInfo.RequiredRoles"/>
    /// list read from options. The metadata is built once, when the <see cref="AgentInfoRegistry"/> is first resolved.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The agent name. It must match the name of the <see cref="AgentInfo"/> that <paramref name="infoFactory"/> returns.</param>
    /// <param name="infoFactory">Builds the agent metadata.</param>
    /// <param name="workflowFactory">Builds the agent's workflow.</param>
    public static IServiceCollection AddRoutableAgent(this IServiceCollection services, string name, Func<IServiceProvider, AgentInfo> infoFactory, Func<IServiceProvider, object?, Workflow> workflowFactory) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        services.AddKeyedScoped(name, workflowFactory);
        services.AddSingleton(sp => {
            var info = infoFactory(sp);
            if (!string.Equals(info.Name, name, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException($"The agent registered as '{name}' describes itself as '{info.Name}'.");
            }
            return info;
        });
        return services;
    }

    /// <summary>
    /// Registers the label shown to the user while a workflow step runs. Use it for the steps of agents registered outside this
    /// package; the built-in steps are labelled by <see cref="AgentMessageLocalizer"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="executorId">The id of the step (executor).</param>
    /// <param name="label">Returns the label in the current UI culture, from the request services.</param>
    public static IServiceCollection AddAgentStepLabel(this IServiceCollection services, string executorId, Func<IServiceProvider, string> label) {
        ArgumentException.ThrowIfNullOrWhiteSpace(executorId);
        ArgumentNullException.ThrowIfNull(label);
        services.AddSingleton(new AgentStepLabel(executorId, label));
        return services;
    }

    /// <summary>
    /// Registers the default text of a prompt template used by an agent registered outside this package. A host can still
    /// override it with <c>Prompts/{name}.txt</c>, exactly like the built-in templates.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The template name passed to <see cref="IPromptTemplateRenderer.Render(string, object?)"/>.</param>
    /// <param name="template">The Handlebars template.</param>
    public static IServiceCollection AddPromptTemplateDefault(this IServiceCollection services, string name, string template) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        services.AddSingleton(new PromptTemplateDefault(name, template));
        return services;
    }

    /// <summary>
    /// Registers the machine-to-machine token client the MCP clients of the agents host authenticate with, named
    /// <see cref="AgentsConstants.McpSecurityClient"/>. It reads <c>General:Endpoints:TokenEndpoint</c> and <c>General:Secrets</c>.
    /// Calling it more than once just repeats the same configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    public static IServiceCollection AddMcpSecurityTokenClient(this IServiceCollection services, IConfiguration configuration) {
        services.AddClientCredentialsTokenManagement()
                .AddClient(AgentsConstants.McpSecurityClient, credentials => {
                    // Machine-to-machine authentication (no user present, no redirect/browser).
                    credentials.TokenEndpoint = new Uri(configuration["General:Endpoints:TokenEndpoint"]!);
                    credentials.ClientId = ClientId.Parse(configuration["General:Secrets:ClientId"]!);
                    credentials.ClientSecret = ClientSecret.Parse(configuration["General:Secrets:ClientSecret"]!);
                    credentials.Scope = Scope.Parse(configuration["General:Secrets:Scope"]!);
                });
        return services;
    }


    /// <summary>
    /// Registers the Cases workflow steps and the composed Cases workflow.
    /// <para>
    /// The OTP verification leg is handled by a single LLM-powered step
    /// that uses the <c>"otp"</c> MCP service tools at runtime, guided by the
    /// <c>CasesOtpAgent</c> prompt template. There are no hardcoded send/validate steps.
    /// </para>
    /// Call after <c>AddAgentsCore(...)</c>.
    /// </summary>
    public static IServiceCollection AddOperatorWorkflow(this IServiceCollection services, IConfiguration configuration, Action<CustomerWorkflowOptions>? configureOptions = null) {
        var optionsBuilder = services.AddOptions<CustomerWorkflowOptions>();
        if (configureOptions is not null) {
            optionsBuilder.Configure(configureOptions);
        }
        // The meta "auto" router advertises itself for discovery but has no workflow of its own — it runs the
        // IntentRouterService to pick one of the routable agents registered below.
        services.AddSingleton(new AgentInfo(
            Name: AgentsConstants.AgentNames.Operator,
            Description: "Retrieves and handles a case by its case or reference ID, using tools and predefined rules.",
            InputContentTypes: ["text/plain"],
             OutputContentTypes: ["text/markdown", "text/html", 
                                  AgentsConstants.MediaTypes.FunctionCallPort.Request],
            Capabilities: [new AgentCapability("Tool invocation", "Run tools and strong customer authentication"),
                           new AgentCapability("Strong customer authentication", "Authenticate customers using strong methods"),
                           new AgentCapability("Data presentation", "Present data to users in the form of html smart cards")],
            Domains: [],
            Tags: ["operator", "workflow"],
            Links: [],
            Icon: AgentsConstants.AgentIcons.Gear));


        services.AddMcpSecurityTokenClient(configuration);
        services.AddMcpClient("id")
                .WithClientCredentialsHttpTransport(new Uri(configuration["General:Endpoints:IdentityMCP"]!), ClientCredentialsClientName.Parse(AgentsConstants.McpSecurityClient));
        services.AddMcpClient("cases")
                .WithClientCredentialsHttpTransport(new Uri(configuration["General:Endpoints:CasesMCP"]!), ClientCredentialsClientName.Parse(AgentsConstants.McpSecurityClient));

        services.TryAddKeyedTransient<ICustomerDataResolver, CaseDataResolver>(CaseDataResolver.ServiceKey);
        services.TryAddTransient<CustomerDataResolverFactory>();
        services.TryAddTransient<ICustomerDataCardRenderer, HandlebarsCustomerDataCardRenderer>();
        services.TryAddTransient<DataRetrieverStep>();
        services.TryAddTransient<AuthenticationChallengeStep>();
        services.TryAddKeyedTransient<IChallengeValidator, DefaultChallengeValidator>(DefaultChallengeValidator.Default);
        services.TryAddTransient<ChallengeValidatorFactory>();
        services.TryAddTransient<AuthenticationStep>();
        services.TryAddTransient<OtpCodeSendStep>();
        services.TryAddTransient<OtpCodeValidatorStep>();
        services.TryAddTransient<DataPresenterStep>();
        services.TryAddTransient<PaymentMethodStep>();
        services.TryAddTransient<PaymentCompletedStep>();

        services.TryAddTransient<OperationSelectionStep>();
        services.TryAddTransient<CaseReferenceResolverStep>();
        services.TryAddTransient<UserInputCollector>();

        services.AddKeyedScoped(AgentsConstants.AgentNames.Operator, (sp, key) => {
            var operationClassifier = sp.GetRequiredService<OperationSelectionStep>();
            var userInputRetriever = sp.GetRequiredService<CaseReferenceResolverStep>();

            var retriever = sp.GetRequiredService<DataRetrieverStep>();
            var ownershipPrompt = sp.GetRequiredService<AuthenticationChallengeStep>();
            var ownershipValidate = sp.GetRequiredService<AuthenticationStep>();
            var otpSend = sp.GetRequiredService<OtpCodeSendStep>();
            var otpValidate = sp.GetRequiredService<OtpCodeValidatorStep>();
            var dataPresenter = sp.GetRequiredService<DataPresenterStep>();
            var paymentMethod = sp.GetRequiredService<PaymentMethodStep>();
            var paymentCompleted = sp.GetRequiredService<PaymentCompletedStep>();

            var ownershipPort = ChallengeRequestPort.Create();
            var otpPort = OtpRequestPort.Create();
            var operationPort = OperationRequestPort.Create();
            var userInputPort = UserInputRequestPort.Create();
            var userInputCollector = sp.GetRequiredService<UserInputCollector>();
            var paymentPort = PaymentRequestPort.Create();

            var builder = new WorkflowBuilder(operationClassifier);
            builder.AddSwitch(operationClassifier, sw => sw
                .AddCase<OperationRequestPort.OperationRequest>(request => request is not null, operationPort)
                .WithDefault(retriever));
            builder.AddEdge(operationPort, userInputRetriever);
            builder.AddSwitch(userInputRetriever, sw => sw
                .AddCase<UserInputRequestPort.UserInputRequest>(request => request is not null, userInputPort)
                .WithDefault(retriever));
            builder.AddEdge(userInputPort, userInputCollector);
            builder.AddEdge(userInputCollector, retriever);
            builder.AddEdge(retriever, ownershipPrompt);
            builder.AddEdge(ownershipPrompt, ownershipPort);
            builder.AddEdge(ownershipPort, ownershipValidate);
            // Important: The switch in MAF is not based on the type of the message, but on the predicate.
            // The predicate is evaluated at runtime to determine which case to execute.
            // The framework passes null to a typed switch predicate when the message type differs.
            // When ported to c# this seems counterintuitive, but the generic argument is necessary to write a meaningful predicate. (MAF original implementation is on python)
            builder.AddSwitch(ownershipValidate, sw => sw
                .AddCase<OperationState>(message => message is not null, otpSend)
                .WithDefault(ownershipPort));

            builder.AddEdge(otpSend, otpPort);
            builder.AddEdge(otpPort, otpValidate);
            builder.AddSwitch(otpValidate, sw => sw
                .AddCase<OperationState>(message => message is not null, dataPresenter)
                .WithDefault(otpPort));

            builder.AddEdge(dataPresenter, paymentMethod);
            builder.AddEdge(paymentMethod, paymentPort);
            builder.AddEdge(paymentPort, paymentCompleted);

            builder.WithOutputFrom(paymentCompleted, ownershipValidate, otpValidate);
            return builder.Build();
        });

        return services;
    }

    /// <summary>Adds an overridden implementation of <see cref="AgentMessageLocalizer"/>.</summary>
    /// <typeparam name="TDescriber">The type of labels describer.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddIAgentMessageLocalizer<TDescriber>(this IServiceCollection services) where TDescriber : AgentMessageLocalizer {
        services.AddScoped<TDescriber>();
        services.AddScoped<AgentMessageLocalizer>(sp => sp.GetRequiredService<TDescriber>());
        return services;
    }
}
