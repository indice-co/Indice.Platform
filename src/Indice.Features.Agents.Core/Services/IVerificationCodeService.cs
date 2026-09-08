using Indice.Features.Agents.Core.Workflows.Verification;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Sends and validates the one-time password that completes strong customer verification, over a channel
/// discovered inside the customer data itself. The default implementation calls MCP tools; hosts with an
/// in-process TOTP service can replace it.
/// </summary>
public interface IVerificationCodeService
{
    /// <summary>Delivers a one-time password to <paramref name="channel"/>. Returns <c>false</c> when delivery failed.</summary>
    /// <param name="channel">A phone or e-mail field taken from the customer data payload.</param>
    /// <param name="conversationId">The conversation the challenge belongs to, so implementations can scope/throttle it.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<bool> SendAsync(VerifiableField channel, string conversationId, CancellationToken cancellationToken = default);

    /// <summary>Validates <paramref name="code"/> against the password last sent to <paramref name="channel"/>.</summary>
    /// <param name="channel">The channel the code was delivered to.</param>
    /// <param name="code">The code as typed by the user.</param>
    /// <param name="conversationId">The conversation the challenge belongs to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<bool> VerifyAsync(VerifiableField channel, string code, string conversationId, CancellationToken cancellationToken = default);
}
