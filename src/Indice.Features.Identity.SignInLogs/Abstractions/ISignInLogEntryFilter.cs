using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Abstractions;

internal interface ISignInLogEntryFilter
{
    Task<bool> Discard(SignInLogEntry logEntry);
}
