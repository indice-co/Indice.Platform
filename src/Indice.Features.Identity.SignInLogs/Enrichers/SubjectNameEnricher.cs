using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

/// <summary>Enriches the sign in log entry with the username.</summary>
public sealed class SubjectNameEnricher : ISignInLogEntryEnricher
{
    private readonly ExtendedUserManager<User> _userManager;

    /// <summary>Creates a new instance of <see cref="SubjectNameEnricher"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
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
