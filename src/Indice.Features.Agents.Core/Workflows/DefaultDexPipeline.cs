using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Features.Agents.Core.Workflows.Reranking;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Features.Agents.Core.Workflows.Steps;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>Registers the canonical Dex RAG pipeline: Intent → Rewrite → Retrieve → Rerank → Compose.</summary>
public static class DefaultDexPipelineExtensions
{
    /// <summary>
    /// Registers the five default steps, the default <see cref="ILlmReranker"/>, and a scoped
    /// <see cref="Workflow"/> wiring them in order. Call after <c>AddDex(...)</c>.
    /// </summary>
    public static IServiceCollection AddDefaultDexPipeline(this IServiceCollection services) {
        services.TryAddTransient<IntentClassifier>();
        services.TryAddTransient<QueryRewriter>();
        services.TryAddTransient<Retriever>();
        services.TryAddTransient<Reranker>();
        services.TryAddTransient<AnswerComposer>();
        services.TryAddTransient<OutOfScopeResponder>();
        services.TryAddTransient<PurposeResponder>();
        services.TryAddTransient<MessageAgent>();
        
        services.TryAddTransient<ILlmReranker, LlmListwiseReranker>();

        // Register the workflow, which will resolve the steps and link them together. Step failures are not
        // handled here — a throwing executor halts the run and DexRunner reads the ExecutorFailedEvent.
        services.AddKeyedScoped<Workflow>("Default",(sp, key) => {
            var intent          = sp.GetRequiredService<IntentClassifier>();
            var rewrite         = sp.GetRequiredService<QueryRewriter>();
            var retrieve        = sp.GetRequiredService<Retriever>();
            var rerank          = sp.GetRequiredService<Reranker>();
            var compose         = sp.GetRequiredService<AnswerComposer>();
            var outOfScopeReply = sp.GetRequiredService<OutOfScopeResponder>();
            var purposeResponder = sp.GetRequiredService<PurposeResponder>();
            var messageAgent    = sp.GetRequiredService<MessageAgent>();
            
            var builder = new WorkflowBuilder(intent);
            builder.AddSwitch(intent, sw => sw
                .AddCase<PipelineStepContext<IntentOutput>>(env => env!.Payload.Intent.Category == "message", messageAgent)
                .AddCase<PipelineStepContext<IntentOutput>>(env => env!.Payload.Intent.Category == "purpose_of_agent", purposeResponder)
                .AddCase<PipelineStepContext<IntentOutput>>(env => env!.Payload.Intent.IsInScope, rewrite)
                .WithDefault(outOfScopeReply));
            builder.AddEdge(rewrite,  retrieve);
            builder.AddEdge(retrieve, rerank);
            builder.AddEdge(rerank,   compose);
            builder.WithOutputFrom(compose, outOfScopeReply, purposeResponder, messageAgent);
            return builder.Build();
        });
        return services;
    }
}