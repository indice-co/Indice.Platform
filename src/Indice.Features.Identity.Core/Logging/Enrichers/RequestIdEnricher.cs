using Indice.Features.Identity.Core.Logging.Abstractions;
using Indice.Features.Identity.Core.Logging.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core.Logging.Enrichers;

internal class RequestIdEnricher : ISignInLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestIdEnricher(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Task Enrich(SignInLogEntry logEntry) {
        logEntry.RequestId = _httpContextAccessor.HttpContext.TraceIdentifier;
        return Task.CompletedTask;
    }
}
