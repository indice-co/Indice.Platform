using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Core;

/// <summary>Options for configuring the IdentityServer audit mechanism.</summary>
public class AuditEventSinkOptions
{
    internal IServiceCollection Services;
}
