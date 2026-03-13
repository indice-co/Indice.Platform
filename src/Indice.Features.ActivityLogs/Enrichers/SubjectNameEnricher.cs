using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>Enriches the activity log entry with the username.</summary>
public sealed class SubjectNameEnricher : IActivityLogEntryEnricher
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
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Asynchronous;

    /// <inheritdoc />
    public async ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        if (!string.IsNullOrWhiteSpace(logEntry.SubjectId)) {
            logEntry.User ??= await _userManager.FindByIdAsync(logEntry.SubjectId);
        }
        if (!string.IsNullOrWhiteSpace(logEntry.User?.UserName)) {
            logEntry.SubjectName = logEntry.User.UserName;
        } 
    }
}
