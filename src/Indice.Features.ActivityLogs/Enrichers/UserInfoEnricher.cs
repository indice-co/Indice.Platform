using System.Security.Claims;
using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Models;
using Indice.Security;
using Microsoft.AspNetCore.Http;

/// <summary>Enriches the activity log entry with basic information such as the subject id.</summary>
public sealed class UserInfoEnricher : IActivityLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="UserInfoEnricher"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public UserInfoEnricher(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public int Order => 3;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public async ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        var context = _httpContextAccessor.HttpContext;
        var user = context?.User;
        if (user is not null) {
            logEntry.SubjectId = user.FindFirstValue(BasicClaimTypes.Subject);
            logEntry.SubjectName = user.FindFirstValue(BasicClaimTypes.Name);
            logEntry.SessionId = user.FindFirstValue(BasicClaimTypes.SessionId);
            logEntry.ApplicationId = user.FindFirstValue(BasicClaimTypes.ClientId);
        }

    }
}