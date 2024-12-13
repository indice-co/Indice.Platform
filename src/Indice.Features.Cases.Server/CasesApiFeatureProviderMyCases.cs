using Indice.Features.Cases.Server.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace Indice.Features.Cases.Server;

/// <summary>Provider for registering my-cases features.</summary>
public static class CasesApiFeatureProviderMyCases
{
    /// <summary>Registers the my-cases features.</summary>
    public static void MapMyCasesEndpoints(this IEndpointRouteBuilder app) {
        app.MapMyCases();
        app.MapMyCaseTypes();
    }
}
