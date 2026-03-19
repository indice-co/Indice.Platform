
using Indice.Features.ActivityLogs.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.ActivityLogs;

/// <inheritdoc/>
public class ActivityLogBuilder(IServiceCollection services, IConfiguration configuration) : IActivityLogBuilder
{
    public IServiceCollection Services { get; } = services;
    public IConfiguration Configuration { get; } = configuration;
}