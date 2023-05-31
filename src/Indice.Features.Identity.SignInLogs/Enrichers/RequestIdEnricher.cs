using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class RequestIdEnricher : ISignInLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestIdEnricher(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public int Order => 2;
    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Synchronous;

    public ValueTask EnrichAsync(SignInLogEntry logEntry) {
        logEntry.RequestId = _httpContextAccessor.HttpContext.TraceIdentifier;
        return ValueTask.CompletedTask;
    }
}
