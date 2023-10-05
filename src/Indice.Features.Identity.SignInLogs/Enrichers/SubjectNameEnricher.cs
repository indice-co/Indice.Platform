using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

/// <summary></summary>
public sealed class SubjectNameEnricher : ISignInLogEntryEnricher
{
    private readonly ExtendedUserManager<User> _userManager;

    /// <summary></summary>
    /// <param name="userManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SubjectNameEnricher(ExtendedUserManager<User> userManager) {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <inheritdoc />
    public int Order => 8;
    /// <inheritdoc />
    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Asynchronous;

    /// <inheritdoc />
    public async ValueTask EnrichAsync(SignInLogEntry logEntry) {
        logEntry.User ??= (!string.IsNullOrWhiteSpace(logEntry.SubjectId) ? await _userManager.FindByIdAsync(logEntry.SubjectId) : default);
        logEntry.SubjectName = logEntry?.User?.UserName;
    }
}
