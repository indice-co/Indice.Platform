using Indice.Features.Agents.Core.Models;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Enforces the usage limits declared in <see cref="SessionOptions"/> before any pipeline work (and cost) is incurred.
/// A limit set to zero or a negative value is disabled.
/// </summary>
public interface IUsageGuardService
{
    /// <summary>
    /// Checks whether a new turn may run on <paramref name="session"/> against
    /// <see cref="SessionOptions.MaxMessagesPerSession"/> and <see cref="SessionOptions.MaxTokensPerSession"/>.
    /// Pure metadata check — the session's counters are already loaded by the send paths.
    /// </summary>
    UsageGuardResult Check(Session session);

    /// <summary>
    /// Checks whether <paramref name="userId"/> may create a new session against
    /// <see cref="SessionOptions.MaxSessionsPerUser"/>.
    /// </summary>
    Task<UsageGuardResult> CheckSessionCreationAsync(string userId, CancellationToken cancellationToken);
}
