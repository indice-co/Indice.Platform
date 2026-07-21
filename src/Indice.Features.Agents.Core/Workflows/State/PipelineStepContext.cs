namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>
/// The message that flows along every pipeline edge. Pairs a typed <typeparamref name="TPayload"/> with the
/// immutable cross-cutting <see cref="State"/>. Steps construct new envelopes via <see cref="Next{TNext}"/>;
/// they never mutate the input.
/// </summary>
/// <typeparam name="TPayload">The payload type for this edge (changes from step to step).</typeparam>
public class PipelineStepContext<TPayload>
{
    /// <summary>The typed payload carried by this edge.</summary>
    public TPayload Payload { get; init; } = default!;

    /// <summary>The cross-cutting state accumulated so far (immutable).</summary>
    public RagState State { get; init; } = new();

    /// <summary>Creates an envelope from a payload (and optionally a starting state).</summary>
    public static PipelineStepContext<TPayload> From(TPayload payload, RagState? state = null)
        => new() { Payload = payload, State = state ?? new RagState() };

    /// <summary>Creates a downstream envelope carrying a new <typeparamref name="TNext"/> payload and the current (or replaced) state.</summary>
    public PipelineStepContext<TNext> Next<TNext>(TNext payload, RagState? state = null)
        => new() { Payload = payload, State = state ?? State };
}
