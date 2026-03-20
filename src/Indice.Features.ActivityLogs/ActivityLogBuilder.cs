
using Indice.Features.ActivityLogs.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.ActivityLogs;

/// <inheritdoc/>
public class ActivityLogBuilder(IServiceCollection services, IConfiguration configuration) : IActivityLogBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services { get; } = services;
    /// <inheritdoc/>
    public IConfiguration Configuration { get; } = configuration;
}