using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class SubjectNameEnricher : ISignInLogEntryEnricher
{
    private readonly ExtendedUserManager<User> _userManager;

    public SubjectNameEnricher(ExtendedUserManager<User> userManager) {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public int Priority => 1;
    public EnricherDependencyType DependencyType => EnricherDependencyType.OnDataStore;

    public async Task EnrichAsync(SignInLogEntry logEntry) {
        if (string.IsNullOrWhiteSpace(logEntry?.SubjectId) || !string.IsNullOrWhiteSpace(logEntry?.SubjectName)) {
            return;
        }
        var user = await _userManager.FindByIdAsync(logEntry.SubjectId);
        logEntry.SubjectName = user?.UserName;
    }
}
