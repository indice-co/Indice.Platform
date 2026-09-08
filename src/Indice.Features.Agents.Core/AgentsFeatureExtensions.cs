using System.ClientModel;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.Reranking;
using Indice.Features.Agents.Core.Workflows.Steps;
using Indice.Features.Agents.Core.Workflows.Steps.CustomerData;
using Indice.Features.Agents.Core.Workflows.Cards;
using Microsoft.Agents.AI.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenAI;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions to configure the Agents feature.</summary>
public static class AgentsFeatureExtensions
{
    /// <summary>
    /// Registers Dex core services: <see cref="AgentsOptions"/> (bound from the <c>Dex</c> configuration section).
    /// </summary>
    public static IServiceCollection AddAgentsCore(this IServiceCollection services, IConfiguration configuration, Action<AgentsOptions>? configureAction = null) {
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
        services.AddKeyedChatClient(nameof(AzureOpenAIDeployments.Reasoning), sp => {
            var opts = sp.GetRequiredService<IOptions<AgentsOptions>>().Value.AzureOpenAI.Deployments;
            var innerClient = sp.GetRequiredService<AzureOpenAIClient>();
            return innerClient.GetChatClient(opts.Reasoning).AsIChatClient();
        });
        services.AddKeyedChatClient(nameof(AzureOpenAIDeployments.Fast), sp => {
            var opts = sp.GetRequiredService<IOptions<AgentsOptions>>().Value.AzureOpenAI.Deployments;
            var innerClient = sp.GetRequiredService<AzureOpenAIClient>();
            return innerClient.GetChatClient(opts.Fast).AsIChatClient();
        });

        services.AddEmbeddingGenerator(sp => {
            var opts = sp.GetRequiredService<IOptions<AgentsOptions>>().Value.AzureOpenAI;
            var innerClient = sp.GetRequiredService<AzureOpenAIClient>();
            return innerClient.GetEmbeddingClient(opts.Deployments.Embedding!).AsIEmbeddingGenerator(opts.EmbeddingDimensions);
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
        services.TryAddTransient<IConversationStore, ConversationStore>();
        services.TryAddTransient<IUsageGuardService, UsageGuardService>();
        services.TryAddTransient<ConversationStoreChatHistoryProvider>();
        services.TryAddSingleton<IPromptTemplateRenderer, FileSystemPromptTemplateRenderer>();
        services.TryAddTransient<IDexChatClient, AgentsChatClient>();
        services.TryAddSingleton<AgentsClaimsPrincipalSelector>(sp =>
            () => null
        );
        services.TryAddSingleton<ISourceLinkGenerator, NoOpSourceLinkGenerator>();
        services.AddAgentsDefaultPipeline();
        services.AddAgentsCustomerDataPipeline();
        return services;
    }

    /// <summary>
    /// Registers the five default steps, the default <see cref="ILlmReranker"/>, and a scoped
    /// <see cref="Workflow"/> wiring them in order. Call after <c>AddDex(...)</c>.
    /// </summary>
    public static IServiceCollection AddAgentsDefaultPipeline(this IServiceCollection services) {
        services.TryAddTransient<IntentClassifier>();
        services.TryAddTransient<QueryRewriter>();
        services.TryAddTransient<Retriever>();
        services.TryAddTransient<Reranker>();
        services.TryAddTransient<AnswerComposer>();
        services.TryAddTransient<OutOfScopeResponder>();
        services.TryAddTransient<PurposeResponder>();
        services.TryAddTransient<ILlmReranker, LlmListwiseReranker>();

        // Register the workflow, which will resolve the steps and link them together. Step failures are not
        // handled here — a throwing executor halts the run and DexRunner reads the ExecutorFailedEvent.
        services.AddKeyedScoped(AgentsConstants.AgentNames.Knowledge, (sp, key) => {
            var intent = sp.GetRequiredService<IntentClassifier>();
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
    /// Registers the customer-data steps, their default services (MCP-backed data retrieval and one-time
    /// password verification, Handlebars card rendering, durable per-conversation workflow state) and two keyed
    /// workflows: <see cref="AgentsConstants.AgentNames.Cases"/>, the sub-workflow on its own, and
    /// <see cref="AgentsConstants.AgentNames.Auto"/>, the master intent classifier routing between the
    /// knowledge pipeline and the customer-data sub-workflow.
    /// </summary>
    /// <remarks>
    /// Every service registered here is a <c>TryAdd</c>, so a host that resolves customer data in-process, signs
    /// one-time passwords itself, or renders its own cards only has to register its own implementation first.
    /// </remarks>
    public static IServiceCollection AddAgentsCustomerDataPipeline(this IServiceCollection services) {
        services.TryAddTransient<IWorkflowStateStore, ConversationWorkflowStateStore>();
        services.TryAddTransient<ICustomerDataResolver, McpCustomerDataResolver>();
        services.TryAddTransient<IVerificationCodeService, McpVerificationCodeService>();
        services.TryAddSingleton<ICustomerDataCardRenderer, HandlebarsCustomerDataCardRenderer>();

        services.TryAddTransient<CustomerDataRouter>();
        services.TryAddTransient<ReferenceCollector>();
        services.TryAddTransient<CustomerDataRetriever>();
        services.TryAddTransient<IdentityVerifier>();
        services.TryAddTransient<OtpVerifier>();
        services.TryAddTransient<CustomerDataCardPresenter>();
        services.TryAddTransient<CustomerDataIntentForwarder>();

        services.AddKeyedScoped(AgentsConstants.AgentNames.Cases, (sp, key) => {
            var router = sp.GetRequiredService<CustomerDataRouter>();
            var builder = new WorkflowBuilder(router);
            return AddCustomerDataChain(builder, sp, router).Build();
        });

        services.AddKeyedScoped(AgentsConstants.AgentNames.Auto, (sp, key) => {
            var intent = sp.GetRequiredService<IntentClassifier>();
            var rewrite = sp.GetRequiredService<QueryRewriter>();
            var retrieve = sp.GetRequiredService<Retriever>();
            var rerank = sp.GetRequiredService<Reranker>();
            var compose = sp.GetRequiredService<AnswerComposer>();
            var outOfScopeReply = sp.GetRequiredService<OutOfScopeResponder>();
            var purposeResponder = sp.GetRequiredService<PurposeResponder>();
            var forwarder = sp.GetRequiredService<CustomerDataIntentForwarder>();
            var router = sp.GetRequiredService<CustomerDataRouter>();

            var builder = new WorkflowBuilder(intent);
            // Cases are evaluated in order: a question about the user's own record never falls through to retrieval.
            builder.AddSwitch(intent, sw => sw
                .AddCase<IntentOutput>(env => env!.Intent.Category == "purpose_of_agent", purposeResponder)
                .AddCase<IntentOutput>(env => env!.Intent.RequiresCustomerData, forwarder)
                .AddCase<IntentOutput>(env => env!.Intent.IsInScope, rewrite)
                .WithDefault(outOfScopeReply));
            builder.AddEdge(rewrite, retrieve);
            builder.AddEdge(retrieve, rerank);
            builder.AddEdge(rerank, compose);
            builder.AddEdge(forwarder, router);
            AddCustomerDataChain(builder, sp, router);
            builder.WithOutputFrom(compose, outOfScopeReply, purposeResponder);
            return builder.Build();
        });
        return services;
    }

    /// <summary>
    /// Wires the customer-data chain onto <paramref name="builder"/>, starting from <paramref name="router"/>:
    /// collect the reference, retrieve the data, verify knowledge of it, verify a one-time password, present the
    /// card. Every edge is conditional on the step being able to continue, so a step that needs something from
    /// the user simply ends the run — the conversation resumes on the next turn from the persisted stage.
    /// </summary>
    private static WorkflowBuilder AddCustomerDataChain(WorkflowBuilder builder, IServiceProvider sp, CustomerDataRouter router) {
        var collect = sp.GetRequiredService<ReferenceCollector>();
        var retrieve = sp.GetRequiredService<CustomerDataRetriever>();
        var identity = sp.GetRequiredService<IdentityVerifier>();
        var otp = sp.GetRequiredService<OtpVerifier>();
        var present = sp.GetRequiredService<CustomerDataCardPresenter>();

        builder.AddEdge<CustomerDataTurn>(router, retrieve, turn => turn?.Continue == true && turn.State.GetReference() is not null);
        builder.AddEdge<CustomerDataTurn>(router, collect, turn => turn?.Continue == true && turn.State.GetReference() is null);
        builder.AddEdge<CustomerDataTurn>(collect, retrieve, turn => turn?.Continue == true);
        builder.AddEdge<CustomerDataTurn>(retrieve, identity, turn => turn?.Continue == true);
        builder.AddEdge<CustomerDataTurn>(identity, otp, turn => turn?.Continue == true);
        builder.AddEdge<CustomerDataTurn>(otp, present, turn => turn?.Continue == true);
        builder.WithOutputFrom(router, collect, retrieve, identity, otp, present);
        return builder;
    }
}
