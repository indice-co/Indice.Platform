using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Filters;

internal class AuthorizeEndpointFilter : ISignInLogEntryFilter
{
    public Task<bool> Discard(SignInLogEntry logEntry) => Task.FromResult(logEntry?.ResourceId.Equals("Authorize", StringComparison.OrdinalIgnoreCase) == true);
}
