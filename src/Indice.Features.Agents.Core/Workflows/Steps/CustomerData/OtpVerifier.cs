using System.Text.RegularExpressions;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Features.Agents.Core.Workflows.Verification;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.CustomerData;

/// <summary>
/// Second half of strong customer verification: sends a one-time password to a channel discovered inside the
/// payload (never to one the user supplies) and validates the code typed back. The channel is only ever shown
/// masked.
/// </summary>
public sealed partial class OtpVerifier : CustomerDataStep<CustomerDataTurn>
{
    private readonly IVerificationCodeService _codes;

    /// <summary>Creates a new <see cref="OtpVerifier"/>.</summary>
    public OtpVerifier(IOptions<AgentsOptions> options, IWorkflowStateStore store, IVerificationCodeService codes)
        : base(nameof(OtpVerifier), options.Value, store) {
        _codes = codes;
    }

    /// <inheritdoc/>
    public override async ValueTask<CustomerDataTurn> HandleAsync(CustomerDataTurn turn, IWorkflowContext context, CancellationToken cancellationToken = default) {
        if (turn.Data is null || !turn.State.IdentityVerified) {
            return turn with { Continue = false };
        }
        if (turn.State.CodeVerified) {
            return turn with { Continue = true };
        }
        var (conversationId, text) = await GetConversationAsync(context, cancellationToken);
        var channels = VerifiableDataExtractor.Extract(turn.Data.Data).Where(field => field.IsChannel).ToList();
        var channel = turn.State.ChannelPath is { } path
            ? channels.FirstOrDefault(field => field.Path == path)
            : channels.OrderBy(field => VerifiableFieldLabels.Priority(field.Kind)).FirstOrDefault();
        if (channel is null) {
            var blocked = await PersistAsync(context, turn.State with { Stage = CustomerDataStage.Blocked }, conversationId, cancellationToken);
            await SayAsync(context, Settings.NoChannelMessage, cancellationToken);
            return new CustomerDataTurn { State = blocked, Continue = false, Answer = Settings.NoChannelMessage };
        }
        // No code is outstanding yet — send one and park the conversation until the user types it back.
        if (turn.State.ChannelPath is null) {
            return await SendAsync(context, turn, channel, conversationId, cancellationToken);
        }
        if (TryExtractCode(text) is { } code && await _codes.VerifyAsync(channel, code, conversationId.ToString(), cancellationToken)) {
            var verified = await PersistAsync(context, turn.State with {
                CodeVerified = true,
                Stage = CustomerDataStage.Present
            }, conversationId, cancellationToken);
            return new CustomerDataTurn { State = verified, Data = turn.Data, Continue = true };
        }
        var attempts = turn.State.FailedAttempts + 1;
        var remaining = Settings.MaxVerificationAttempts - attempts;
        var isBlocked = remaining <= 0;
        var state = await PersistAsync(context, turn.State with {
            FailedAttempts = attempts,
            Stage = isBlocked ? CustomerDataStage.Blocked : CustomerDataStage.VerifyCode
        }, conversationId, cancellationToken);
        var message = isBlocked ? Settings.BlockedMessage : string.Format(Settings.CodeMismatchMessage, remaining);
        await SayAsync(context, message, cancellationToken);
        return new CustomerDataTurn { State = state, Data = turn.Data, Continue = false, Answer = message };
    }

    /// <summary>Delivers a one-time password to <paramref name="channel"/> and tells the user where it went, masked.</summary>
    private async Task<CustomerDataTurn> SendAsync(IWorkflowContext context, CustomerDataTurn turn, VerifiableField channel, Guid conversationId, CancellationToken cancellationToken) {
        var sent = await _codes.SendAsync(channel, conversationId.ToString(), cancellationToken);
        if (!sent) {
            await SayAsync(context, Settings.CodeDeliveryFailedMessage, cancellationToken);
            return new CustomerDataTurn { State = turn.State, Data = turn.Data, Continue = false, Answer = Settings.CodeDeliveryFailedMessage };
        }
        var state = await PersistAsync(context, turn.State with {
            Stage = CustomerDataStage.VerifyCode,
            ChannelKind = channel.Kind,
            ChannelPath = channel.Path
        }, conversationId, cancellationToken);
        var message = string.Format(Settings.CodeSentMessage, ChannelMasker.Mask(channel));
        await SayAsync(context, message, cancellationToken);
        return new CustomerDataTurn { State = state, Data = turn.Data, Continue = false, Answer = message };
    }

    /// <summary>Picks the one-time password out of a free-text answer such as "my code is 123456".</summary>
    public static string? TryExtractCode(string? text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return null;
        }
        var match = CodeRegex().Match(text);
        return match.Success ? match.Value : null;
    }

    [GeneratedRegex(@"\d{4,8}", RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();
}
