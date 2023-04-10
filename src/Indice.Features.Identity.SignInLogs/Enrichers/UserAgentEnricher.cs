using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class UserAgentEnricher : ISignInLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public int Priority => 4;
    public EnricherDependencyType DependencyType => EnricherDependencyType.OnRequest;

    public UserAgentEnricher(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Task EnrichAsync(SignInLogEntry logEntry) {
        var userAgentHeader = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.UserAgent];
        logEntry.ExtraData.UserAgent = userAgentHeader;
        return Task.CompletedTask;
    }
}
