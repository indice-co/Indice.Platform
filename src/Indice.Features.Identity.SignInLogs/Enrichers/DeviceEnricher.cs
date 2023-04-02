using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Types;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class DeviceEnricher : ISignInLogEntryEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMfaDeviceIdResolver _mfaDeviceIdResolver;

    public int Priority => 6;
    public EnricherDependencyType DependencyType => EnricherDependencyType.OnRequest;

    public DeviceEnricher(
        IHttpContextAccessor httpContextAccessor,
        IMfaDeviceIdResolver mfaDeviceIdResolver
    ) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _mfaDeviceIdResolver = mfaDeviceIdResolver ?? throw new ArgumentNullException(nameof(mfaDeviceIdResolver));
    }

    public async Task EnrichAsync(SignInLogEntry logEntry) {
        var deviceId = await _mfaDeviceIdResolver.Resolve();
        logEntry.DeviceId = deviceId;
        var userAgentHeader = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.UserAgent];
        if (string.IsNullOrWhiteSpace(userAgentHeader)) {
            return;
        }
        var userAgent = new UserAgent(userAgentHeader);
        logEntry.ExtraData.Device = new { 
            Id = deviceId,
            Model = userAgent.DeviceModel,
            Name = userAgent.DisplayName,
            Platform = userAgent.DevicePlatform,
            UserAgent = userAgent.Raw,
            userAgent.Os
        };
    }
}
