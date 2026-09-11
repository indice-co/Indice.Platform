using System.ClientModel;
using Azure.AI.OpenAI;
using Duende.AccessTokenManagement;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.RequestPorts;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.Reranking;
using Indice.Features.Agents.Core.Workflows.Steps;
using Indice.Features.Agents.Core.Workflows.Steps.Operator;
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
        services.TryAddTransient<IntentRouterService>();
        services.TryAddScoped<AgentMessageLocalizer>();
        services.AddAgentsDefaultPipeline();
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
    /// Registers a routable agent: a keyed <see cref="Workflow"/> resolved by <paramref name="info"/>'s name, plus
    /// its <see cref="AgentInfo"/> metadata. The metadata is collected by <see cref="AgentInfoRegistry"/> (and the
    /// discovery endpoint) through <c>IEnumerable&lt;AgentInfo&gt;</c>. Plain <c>AddSingleton</c> registers the
    /// metadata on purpose: every <see cref="AgentInfo"/> shares one implementation type, so <c>TryAddEnumerable</c>
    /// would dedupe them down to a single entry.
    /// </summary>
    private static IServiceCollection AddRoutableAgent(this IServiceCollection services, AgentInfo info, Func<IServiceProvider, object?, Workflow> workflowFactory) {
        services.AddKeyedScoped(info.Name, workflowFactory);
        services.AddSingleton(info);
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
    public static IServiceCollection AddOperatorWorkflow(this IServiceCollection services, IConfiguration configuration) {

        // The meta "auto" router advertises itself for discovery but has no workflow of its own — it runs the
        // IntentRouterService to pick one of the routable agents registered below.
        services.AddSingleton(new AgentInfo(
            Name: AgentsConstants.AgentNames.Operator,
            Description: "This is an operator agent that can handle tools and provide solutions based on predefined rules.",
            InputContentTypes: ["text/plain"],
            OutputContentTypes: ["text/markdown", AgentsConstants.MediaTypes.MultipleChoice, AgentsConstants.MediaTypes.Callout,
                                 AgentsConstants.MediaTypes.Image, AgentsConstants.MediaTypes.Confirmation, "image/png"],
            Capabilities: [new AgentCapability("Tool invocation", "Run tools and strong customer authentication"),
                           new AgentCapability("Strong customer authentication", "Authenticate customers using strong methods"),
                           new AgentCapability("Data presentation", "Present data to users in the form of html smart cards")],
            Domains: [],
            Tags: ["operator", "workflow"],
            Links: [],
            Icon: AgentsConstants.AgentIcons.Gear));


        services.AddClientCredentialsTokenManagement()
                .AddClient("mcpsecurity", credentials => {
                    // Machine-to-machine authentication (no user present, no redirect/browser).
                    credentials.TokenEndpoint = new Uri(configuration["General:Endpoints:TokenEndpoint"]!);
                    credentials.ClientId = ClientId.Parse(configuration["General:Secrets:ClientId"]!);
                    credentials.ClientSecret = ClientSecret.Parse(configuration["General:Secrets:ClientSecret"]!);
                    credentials.Scope = Scope.Parse(configuration["General:Secrets:Scope"]!);
                });
        services.AddMcpClient("id")
                .WithClientCredentialsHttpTransport(new Uri(configuration["General:Endpoints:IdentityMCP"]!), ClientCredentialsClientName.Parse("mcpsecurity"));
        services.AddMcpClient("cases")
                .WithClientCredentialsHttpTransport(new Uri(configuration["General:Endpoints:CasesMCP"]!), ClientCredentialsClientName.Parse("mcpsecurity"));

        services.TryAddTransient<ICustomerDataResolver, DefaultCustomerDataResolver>();
        services.TryAddTransient<ICasePresentationFormatter, DefaultCasePresentationFormatter>();
        services.TryAddTransient<DataRetrieverStep>();
        services.TryAddTransient<OwnershipVerifierStep>();
        services.TryAddTransient<OwnershipValidatorStep>();
        services.TryAddTransient<OwnershipRetryChallengeBuilder>();
        services.TryAddTransient<OtpCodeSendStep>();
        services.TryAddTransient<OtpCodeValidatorStep>();
        services.TryAddTransient<OtpRetryChallengeBuilder>();
        services.TryAddTransient<DataPresenterStep>();
        services.TryAddTransient<OwnershipVerificationFailureHandler>();
        services.AddKeyedTransient<IRequestPortHandler, OwnershipConfirmationRequestPortHandler>(AgentsConstants.WorkflowPorts.OwnershipConfirmation);
        services.AddKeyedTransient<IRequestPortHandler, OtpVerificationRequestPortHandler>(AgentsConstants.WorkflowPorts.OtpVerification);

        // Request ports for checkpoint-based pause/resume.
        var ownershipPort = RequestPort.Create<OwnershipVerificationOutput, OwnershipConfirmationResponse>(AgentsConstants.WorkflowPorts.OwnershipConfirmation);
        var otpPort = RequestPort.Create<OtpChallengeOutput, OtpCodeResponse>(AgentsConstants.WorkflowPorts.OtpVerification);

        // Cases workflow with native pause/resume through request ports + checkpoints.
        //   CaseDataRetriever -> OwnershipVerifier -> OwnershipConfirmationPort
        //                       -> OwnershipValidator -> (valid) OtpCodeSendStep -> OtpVerificationPort
        //                                            -> (invalid retry) OwnershipRetryChallengeBuilder -> OwnershipConfirmationPort
        //                                            -> (max) OwnershipVerificationFailureHandler
        //   OtpVerificationPort -> OtpCodeValidator -> (valid) CasePresenterStep
        //                                          -> (invalid retry) OtpRetryChallengeBuilder -> OtpVerificationPort
        //                                          -> (max) CasePresenterStep
        services.AddKeyedScoped(AgentsConstants.AgentNames.Operator, (sp, key) => {
            var retriever = sp.GetRequiredService<DataRetrieverStep>();
            var ownnershipVerifier = sp.GetRequiredService<OwnershipVerifierStep>();
            var ownnershipValidator = sp.GetRequiredService<OwnershipValidatorStep>();
            var ownershipRetry = sp.GetRequiredService<OwnershipRetryChallengeBuilder>();
            var ownershipErr = sp.GetRequiredService<OwnershipVerificationFailureHandler>();
            var maxOwnershipValidationAttempts = sp.GetRequiredService<IOptions<AgentsOptions>>().Value.CasesWorkflow.MaxOwnershipValidationAttempts;
            var otpAgent = sp.GetRequiredService<OtpCodeSendStep>();
            var otpValidator = sp.GetRequiredService<OtpCodeValidatorStep>();
            var otpRetry = sp.GetRequiredService<OtpRetryChallengeBuilder>();
            var dataPresenter = sp.GetRequiredService<DataPresenterStep>();


            var builder = new WorkflowBuilder(retriever);
            builder.AddEdge(retriever, ownnershipVerifier);
            builder.AddEdge(ownnershipVerifier, ownershipPort);
            builder.AddEdge(ownershipPort, ownnershipValidator);
            builder.AddSwitch(ownnershipValidator, sw => sw
                .AddCase<UserInputValidationOutput>(env => env!.IsValid, otpAgent)
                .AddCase<UserInputValidationOutput>(env => !env!.IsValid && env.ValidationAttempt < maxOwnershipValidationAttempts, ownershipRetry)
                .WithDefault(ownershipErr));
            builder.AddEdge(ownershipRetry, ownershipPort);
            builder.AddEdge(otpAgent, otpPort);

            builder.AddEdge(otpPort, otpValidator);
            builder.AddSwitch(otpValidator, sw => sw
                .AddCase<OtpValidationOutput>(env => env!.IsValid, dataPresenter)
                .AddCase<OtpValidationOutput>(env => env!.ShouldRetry, otpRetry)
                .WithDefault(dataPresenter));
            builder.AddEdge(otpRetry, otpPort);

            builder.WithOutputFrom(dataPresenter, ownershipErr, ownershipPort, otpPort);
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
