using System.Runtime.CompilerServices;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.Abstractions;

namespace Indice.Features.Agents.Core.Workflows;

/// <inheritdoc/>
public class DexRunner : IDexRunner
{
    private readonly Workflow? workflow;

    /// <summary>
    /// Creates a new <see cref="DexRunner"/> instance.
    /// </summary>
    /// <param name="workflow">The workflow instance to execute.</param>
    public DexRunner ([FromKeyedServices("Default")] Workflow? workflow) {
        this.workflow = workflow;
    }

    /// <summary>Human-friendly progress labels keyed by executor id, surfaced as SSE <c>step</c> events.</summary>
    private static readonly IReadOnlyDictionary<string, string> StepLabels = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["IntentClassifier"]    = "Classifying intent",
        ["QueryRewriter"]       = "Rewriting query",
        ["Retriever"]           = "Retrieving relevant context",
        ["Reranker"]            = "Ranking results",
        ["AnswerComposer"]      = "Composing answer",
        ["PurposeResponder"]    = "Answering",
        ["OutOfScopeResponder"] = "Preparing response",
    };

    /// <inheritdoc/>
    public async Task<RagResult> RunAsync(RagRequest request, CancellationToken cancellationToken) {
        var initial = CreateInitialEnvelope(request);
        await using var run = await InProcessExecution.RunAsync(workflow!, initial, cancellationToken: cancellationToken);
        PipelineStepContext<RagPipelineOutput>? final = null;
        string? failure = null;
        UsageDetails? usage = null;
        string? modelUsed = null;
        foreach (var evt in run.NewEvents) {
            switch (evt) {
                // The terminal executors (compose / out-of-scope) are registered via WithOutputFrom, so their
                // returned envelope is yielded as a WorkflowOutputEvent — MAF's dedicated terminal-output channel.
                case WorkflowOutputEvent { Data: PipelineStepContext<RagPipelineOutput> env }:
                    final = env;
                    break;
                // Each LLM step reports its own call usage; fold into a single run total.
                case UsageEvent usageEvent:
                    (usage ??= new UsageDetails()).Add(usageEvent.Details);
                    modelUsed = usageEvent.Model;
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
            Citations = final?.Payload?.Citations ?? Array.Empty<Models.Citation>(),
            Failed = failure is not null,
            FailureReason = failure,
            Usage = usage,
            ModelUsed = modelUsed,
        };
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<DexStreamEvent> RunStreamingAsync(
        RagRequest request, [EnumeratorCancellation] CancellationToken cancellationToken) {
        var initial = CreateInitialEnvelope(request);
        await using var run = await InProcessExecution.RunStreamingAsync(workflow!, initial, cancellationToken: cancellationToken);
        PipelineStepContext<RagPipelineOutput>? final = null;
        string? failure = null;
        UsageDetails? usage = null;
        string? modelUsed = null;
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                // One progress event per step start; unmapped executor ids are skipped.
                case ExecutorInvokedEvent invoked when StepLabels.TryGetValue(invoked.ExecutorId, out var label):
                    yield return new DexStepEvent(invoked.ExecutorId, label);
                    break;
                // Each LLM step reports its own call usage; fold into a single run total.
                case UsageEvent usageEvent:
                    (usage ??= new UsageDetails()).Add(usageEvent.Details);
                    modelUsed = usageEvent.Model;
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
            Citations = final?.Payload?.Citations ?? Array.Empty<Models.Citation>(),
            Failed = failure is not null,
            FailureReason = failure,
            Usage = usage,
            ModelUsed = modelUsed,
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
            SessionId = request.SessionId,
        };
        return PipelineStepContext<RagPipelineInput>.From(new RagPipelineInput(), initialState);
    }
}
