using System.Text.RegularExpressions;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.CustomerData;

/// <summary>
/// Establishes the external reference the rest of the flow works on. When the conversation was grounded by the
/// host UI (<c>?refid=...&amp;reftype=...</c>) this step never runs; otherwise it picks the identifier out of
/// what the user typed, and asks for it when there is nothing to pick.
/// </summary>
public sealed partial class ReferenceCollector : CustomerDataStep<CustomerDataTurn>
{
    /// <summary>Creates a new <see cref="ReferenceCollector"/>.</summary>
    public ReferenceCollector(IOptions<AgentsOptions> options, IWorkflowStateStore store) : base(nameof(ReferenceCollector), options.Value, store) { }

    /// <inheritdoc/>
    public override async ValueTask<CustomerDataTurn> HandleAsync(CustomerDataTurn turn, IWorkflowContext context, CancellationToken cancellationToken = default) {
        var (conversationId, text) = await GetConversationAsync(context, cancellationToken);
        if (TryExtractReference(text) is not { } referenceId) {
            var state = await PersistAsync(context, turn.State with { Stage = CustomerDataStage.CollectReference }, conversationId, cancellationToken);
            await SayAsync(context, Settings.AskForReferenceMessage, cancellationToken);
            return new CustomerDataTurn { State = state, Continue = false, Answer = Settings.AskForReferenceMessage };
        }
        // A reference typed mid-conversation replaces whatever was verified before: verification is per record.
        var next = await PersistAsync(context, new CustomerDataState {
            ReferenceId = referenceId,
            ReferenceType = turn.State.ReferenceType
        }, conversationId, cancellationToken);
        return new CustomerDataTurn { State = next, Continue = true };
    }

    /// <summary>
    /// Returns the identifier-looking token of <paramref name="text"/> — a run of at least four letters, digits,
    /// dashes or slashes containing a digit — or <c>null</c> when the user said nothing that could be one.
    /// </summary>
    public static string? TryExtractReference(string? text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return null;
        }
        foreach (Match match in ReferenceRegex().Matches(text)) {
            var candidate = match.Value.Trim('-', '/');
            if (candidate.Length >= 4 && candidate.Any(char.IsAsciiDigit)) {
                return candidate;
            }
        }
        return null;
    }

    [GeneratedRegex(@"[A-Za-z0-9][A-Za-z0-9\-/]{3,31}", RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceRegex();
}
