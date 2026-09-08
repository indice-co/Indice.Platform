using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Features.Agents.Core.Workflows.Verification;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.CustomerData;

/// <summary>
/// First half of strong customer verification: asks the person to prove knowledge of something that is already
/// inside the retrieved payload — a license plate, a customer code, an address — and matches their answer
/// against it without ever disclosing what is on file.
/// </summary>
public sealed class IdentityVerifier : CustomerDataStep<CustomerDataTurn>
{
    /// <summary>Number of different things the user is offered to answer with.</summary>
    private const int MaximumOfferedKinds = 3;

    /// <summary>Creates a new <see cref="IdentityVerifier"/>.</summary>
    public IdentityVerifier(IOptions<AgentsOptions> options, IWorkflowStateStore store) : base(nameof(IdentityVerifier), options.Value, store) { }

    /// <inheritdoc/>
    public override async ValueTask<CustomerDataTurn> HandleAsync(CustomerDataTurn turn, IWorkflowContext context, CancellationToken cancellationToken = default) {
        if (turn.State.IdentityVerified || turn.Data is null) {
            return turn with { Continue = turn.State.IdentityVerified && turn.Data is not null };
        }
        var (conversationId, text) = await GetConversationAsync(context, cancellationToken);
        var candidates = GetChallengeFields(turn.Data);
        if (candidates.Count == 0) {
            // Nothing on this record can identify anyone; refuse rather than present unverified data.
            var blocked = await PersistAsync(context, turn.State with { Stage = CustomerDataStage.Blocked }, conversationId, cancellationToken);
            await SayAsync(context, Settings.BlockedMessage, cancellationToken);
            return new CustomerDataTurn { State = blocked, Continue = false, Answer = Settings.BlockedMessage };
        }
        // First pass through the step for this record: ask, then park the conversation until the next turn.
        if (turn.State.Stage != CustomerDataStage.VerifyIdentity) {
            var offered = candidates.Select(field => field.Kind).Distinct()
                .OrderBy(VerifiableFieldLabels.Priority)
                .Take(MaximumOfferedKinds);
            var question = string.Format(Settings.AskForIdentityMessage, VerifiableFieldLabels.Describe(offered));
            var asked = await PersistAsync(context, turn.State with { Stage = CustomerDataStage.VerifyIdentity }, conversationId, cancellationToken);
            await SayAsync(context, question, cancellationToken);
            return new CustomerDataTurn { State = asked, Data = turn.Data, Continue = false, Answer = question };
        }
        if (VerificationMatcher.Match(candidates, text) is not null) {
            var verified = await PersistAsync(context, turn.State with {
                IdentityVerified = true,
                Stage = CustomerDataStage.VerifyCode
            }, conversationId, cancellationToken);
            return new CustomerDataTurn { State = verified, Data = turn.Data, Continue = true };
        }
        return await RejectAsync(context, turn, conversationId, Settings.IdentityMismatchMessage, cancellationToken);
    }

    /// <summary>Counts the failed answer and either invites another attempt or blocks the conversation.</summary>
    private async Task<CustomerDataTurn> RejectAsync(IWorkflowContext context, CustomerDataTurn turn, Guid conversationId, string mismatchMessage, CancellationToken cancellationToken) {
        var attempts = turn.State.FailedAttempts + 1;
        var remaining = Settings.MaxVerificationAttempts - attempts;
        var blocked = remaining <= 0;
        var state = await PersistAsync(context, turn.State with {
            FailedAttempts = attempts,
            Stage = blocked ? CustomerDataStage.Blocked : turn.State.Stage
        }, conversationId, cancellationToken);
        var message = blocked ? Settings.BlockedMessage : string.Format(mismatchMessage, remaining);
        await SayAsync(context, message, cancellationToken);
        return new CustomerDataTurn { State = state, Data = turn.Data, Continue = false, Answer = message };
    }

    /// <summary>
    /// The fields a person can be challenged on: everything verifiable in the payload except values the user
    /// already demonstrated — the reference they opened the conversation with, and case numbers in general.
    /// </summary>
    public static IReadOnlyList<VerifiableField> GetChallengeFields(CustomerDataRecord record) {
        var reference = VerificationMatcher.Normalize(VerifiableFieldKind.CaseNumber, record.Reference.Id);
        return [.. VerifiableDataExtractor.Extract(record.Data)
            .Where(field => field.Kind != VerifiableFieldKind.CaseNumber)
            .Where(field => !string.Equals(VerificationMatcher.Normalize(field), reference, StringComparison.Ordinal))];
    }
}
