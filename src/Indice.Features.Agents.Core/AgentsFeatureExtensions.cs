using System.ClientModel;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.Reranking;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Features.Agents.Core.Workflows.Steps;
using Indice.Features.Agents.Core.Workflows.Steps.Cases;
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

        services.TryAddSingleton<IMcpToolsRegistry, McpToolsRegistry>();
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
        services.AddCasesWorkflow();
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
    /// Registers the Cases workflow steps and the composed Cases workflow.
    /// <para>
    /// The OTP verification leg is handled by a single LLM-powered <see cref="OtpAgent"/> step
    /// that uses the <c>"otp"</c> MCP service tools at runtime, guided by the
    /// <c>CasesOtpAgent</c> prompt template. There are no hardcoded send/validate steps.
    /// </para>
    /// Call after <c>AddAgentsCore(...)</c>.
    /// </summary>
    public static IServiceCollection AddCasesWorkflow(this IServiceCollection services)
    {
        services.TryAddTransient<CaseDataRetriever>();
        services.TryAddTransient<OwnershipVerifier>();
        services.TryAddTransient<UserInputValidator>();
        services.TryAddTransient<OtpAgent>();
        services.TryAddTransient<OtpCodeValidator>();
        services.TryAddTransient<CaseDataPresenter>();
        services.TryAddTransient<OwnershipVerificationFailureHandler>();
        // Checkpointing infrastructure so the workflow can halt awaiting user input and resume on the next message.
        services.TryAddSingleton<DistributedCacheCheckpointStore>();
        services.TryAddSingleton(sp => CheckpointManager.CreateJson(sp.GetRequiredService<DistributedCacheCheckpointStore>()));

        // Cases workflow:
        //   CaseDataRetriever → OwnershipVerifier → [OwnershipConfirmationPort: halts, asks user] → UserInputValidator
        //       ├─ [valid]   → OtpAgent → [OtpVerificationPort: halts, asks user] → OtpCodeValidator → CaseDataPresenter (terminal)
        //       └─ [invalid] → OwnershipVerificationFailureHandler                                                  (terminal)
        services.AddKeyedScoped(AgentsConstants.AgentNames.Cases, (sp, key) =>
        {
            var retriever    = sp.GetRequiredService<CaseDataRetriever>();
            var verifier     = sp.GetRequiredService<OwnershipVerifier>();
            var validator    = sp.GetRequiredService<UserInputValidator>();
            var otpAgent      = sp.GetRequiredService<OtpAgent>();
            var otpValidator   = sp.GetRequiredService<OtpCodeValidator>();
            var casePresenter  = sp.GetRequiredService<CaseDataPresenter>();
            var ownershipErr   = sp.GetRequiredService<OwnershipVerificationFailureHandler>();
            // External input ports: workflow pauses and host resumes with user responses.
            var confirmationPort = RequestPort.Create<OwnershipVerificationOutput, OwnershipConfirmationResponse>(AgentsConstants.OwnershipConfirmationPortId);
            var otpPort = RequestPort.Create<OtpChallengeOutput, OtpCodeResponse>(AgentsConstants.OtpVerificationPortId);

            var builder = new WorkflowBuilder(retriever);
            builder.AddEdge(retriever, verifier);
            builder.AddEdge(verifier, confirmationPort);
            builder.AddEdge(confirmationPort, validator);

            builder.AddSwitch(validator, sw => sw
                .AddCase<UserInputValidationOutput>(env => env!.IsValid, otpAgent)
                .WithDefault(ownershipErr));

            builder.AddEdge(otpAgent, otpPort);
            builder.AddEdge(otpPort, otpValidator);
            builder.AddEdge(otpValidator, casePresenter);

            builder.WithOutputFrom(casePresenter, ownershipErr);
            return builder.Build();
        });

        return services;
    }
}
