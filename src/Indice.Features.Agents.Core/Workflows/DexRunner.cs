using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.Usage;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Workflows;

/// <inheritdoc/>
public class DexRunner : IDexRunner
{
    private readonly Workflow? workflow;
    private readonly TokenUsageAccumulator usage;

    /// <summary>
    /// Creates a new <see cref="DexRunner"/> instance.
    /// </summary>
    /// <param name="workflow">The workflow instance to execute.</param>
    /// <param name="usage">The token usage accumulator.</param>
    public DexRunner ([FromKeyedServices("Default")] Workflow? workflow, TokenUsageAccumulator usage) {
        this.workflow = workflow;
        this.usage = usage;
    }

    /// <summary>Human-friendly progress labels keyed by executor id, surfaced as SSE <c>step</c> events.</summary>
    private static readonly IReadOnlyDictionary<string, string> StepLabels = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["IntentClassifier"]    = "Classifying intent",
        ["QueryRewriter"]       = "Rewriting query",
        ["Retriever"]           = "Retrieving relevant context",
        ["Reranker"]            = "Ranking results",
        ["AnswerComposer"]      = "Composing answer",
        ["OutOfScopeResponder"] = "Preparing response",
    };

    /// <inheritdoc/>
    public async Task<RagResult> RunAsync(RagRequest request, CancellationToken cancellationToken) {
        var initial = CreateInitialEnvelope(request);
        await using var run = await InProcessExecution.RunAsync(workflow!, initial, cancellationToken: cancellationToken);
        PipelineStepContext<RagPipelineOutput>? final = null;
        string? failure = null;
        foreach (var evt in run.NewEvents) {
            switch (evt) {
                // The terminal executors (compose / out-of-scope) are registered via WithOutputFrom, so their
                // returned envelope is yielded as a WorkflowOutputEvent — MAF's dedicated terminal-output channel.
                case WorkflowOutputEvent { Data: PipelineStepContext<RagPipelineOutput> env }:
                    final = env;
                    break;
                // A throwing step halts the run; MAF surfaces the original exception here, followed by a
                // WorkflowErrorEvent wrapping it — keep the first (richer) message.
                case ExecutorFailedEvent failed:
                    failure ??= $"{failed.ExecutorId}: {failed.Data?.Message ?? "unknown error"}";
                    break;
                case WorkflowErrorEvent error:
                    failure ??= (error.Data as Exception)?.Message ?? "Workflow failed without exception details.";
                    break;
            }
        }
        // Cancellation never surfaces as a failure event — the run just stops emitting events — so check the
        // caller's token explicitly and report a cancellation rather than a pipeline failure.
        cancellationToken.ThrowIfCancellationRequested();
        if (final is null && failure is null) {
            throw new InvalidOperationException("Workflow completed without emitting a final RagPipelineOutput envelope.");
        }
        return new RagResult {
            Answer = final?.Payload?.Answer,
            Citations = final?.Payload?.Citations ?? Array.Empty<Citation>(),
            Failed = failure is not null,
            FailureReason = failure,
            PromptTokens = usage.PromptTokens,
            CompletionTokens = usage.CompletionTokens,
            ModelUsed = usage.Model,
        };
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<DexStreamEvent> RunStreamingAsync(
        RagRequest request, [EnumeratorCancellation] CancellationToken cancellationToken) {
        var initial = CreateInitialEnvelope(request);
        await using var run = await InProcessExecution.RunStreamingAsync(workflow!, initial, cancellationToken: cancellationToken);
        PipelineStepContext<RagPipelineOutput>? final = null;
        string? failure = null;
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                // One progress event per step start; unmapped executor ids are skipped.
                case ExecutorInvokedEvent invoked when StepLabels.TryGetValue(invoked.ExecutorId, out var label):
                    yield return new DexStepEvent(invoked.ExecutorId, label);
                    break;
                // Answer text deltas emitted by AnswerComposer as the reasoning model streams.
                case AnswerDeltaEvent delta when delta.Delta.Length > 0:
                    yield return new DexDeltaEvent(delta.Delta);
                    break;
                // Terminal output from compose / out-of-scope (registered via WithOutputFrom).
                case WorkflowOutputEvent { Data: PipelineStepContext<RagPipelineOutput> env }:
                    final = env;
                    break;
                // A throwing step halts the run; keep the first (richer) message.
                case ExecutorFailedEvent failed:
                    failure ??= $"{failed.ExecutorId}: {failed.Data?.Message ?? "unknown error"}";
                    break;
                case WorkflowErrorEvent error:
                    failure ??= (error.Data as Exception)?.Message ?? "Workflow failed without exception details.";
                    break;
            }
        }
        // Cancellation just stops the stream rather than raising a failure event — surface it as cancellation.
        cancellationToken.ThrowIfCancellationRequested();
        yield return new DexFinalEvent {
            Answer = final?.Payload?.Answer,
            Citations = final?.Payload?.Citations ?? Array.Empty<Citation>(),
            Failed = failure is not null,
            FailureReason = failure,
            PromptTokens = usage.PromptTokens,
            CompletionTokens = usage.CompletionTokens,
            ModelUsed = usage.Model,
        };
    }

    /// <summary>Validates a workflow is registered and builds the initial pipeline envelope from <paramref name="request"/>.</summary>
    private PipelineStepContext<RagPipelineInput> CreateInitialEnvelope(RagRequest request) {
        if (workflow is null) {
            throw new InvalidOperationException(
                "No RAG workflow registered. Call services.AddDefaultDexPipeline() or register a Microsoft.Agents.AI.Workflows.Workflow manually.");
        }
        var initialState = new RagState {
            Question = request.Question,
            History = request.History?.ToImmutableList() ?? ImmutableList<ChatMessage>.Empty,
        };
        return PipelineStepContext<RagPipelineInput>.From(new RagPipelineInput(), initialState);
    }
}
