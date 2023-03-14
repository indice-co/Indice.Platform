using Indice.AspNetCore.Extensions;
using Indice.Features.Identity.Core.Logging.Abstractions;
using Indice.Features.Identity.Core.Logging.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core.Logging.Enrichers;

internal class IpAddressEnricher : ISignInLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IpAddressEnricher(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Task Enrich(SignInLogEntry logEntry) {
        logEntry.IpAddress = _httpContextAccessor.HttpContext.GetClientIpAddress();
        logEntry.IpAddress = "212.205.254.62";
        return Task.CompletedTask;
    }
}
