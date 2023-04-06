using IdentityModel;
using Indice.AspNetCore.Http.Filters;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing Indice Identity Server client applications.</summary>
public static class ClientsApi
{
    /// <summary>Maps the endpoints for managing Indice Identity Server client applications.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapManageClients(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/clients");
        group.WithTags("Clients");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope, IdentityEndpoints.SubScopes.Clients }.Where(x => x != null).ToArray();
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        );
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("", ClientHandlers.GetClients)
             .WithName(nameof(ClientHandlers.GetClients))
             .WithSummary($"Returns a list of {nameof(ClientInfo)} objects containing the total number of claim types in the database and the data filtered according to the provided ListOptions.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader)
             .NoCache();

        group.MapGet("{clientId}", ClientHandlers.GetClient)
             .WithName(nameof(ClientHandlers.GetClient))
             .WithSummary("Gets a client by it's unique id.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader)
             .InvalidateCache(nameof(ClientHandlers.GetClient))
             .CacheOutputMemory();

        group.MapPost("", ClientHandlers.CreateClient)
             .WithName(nameof(ClientHandlers.CreateClient))
             .WithSummary("Creates a new client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(DashboardHandlers.GetSystemSummary), JwtClaimTypes.Subject)
             .WithParameterValidation<CreateClientRequest>();

        group.MapPut("{clientId}", ClientHandlers.UpdateClient)
             .WithName(nameof(ClientHandlers.UpdateClient))
             .WithSummary("Updates an existing client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .WithParameterValidation<UpdateClientRequest>();

        group.MapPost("{clientId}/claims", ClientHandlers.AddClientClaim)
             .WithName(nameof(ClientHandlers.AddClientClaim))
             .WithSummary("Adds a claim for the specified client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ClientHandlers.GetClient))
             .WithParameterValidation<CreateClaimRequest>();

        group.MapDelete("{clientId}/claims/{claimId:int}", ClientHandlers.DeleteClientClaim)
             .WithName(nameof(ClientHandlers.DeleteClientClaim))
             .WithSummary("Removes an identity resource from the specified client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ClientHandlers.GetClient));

        group.MapPut("{clientId}/urls", ClientHandlers.UpdateClientUrls)
             .WithName(nameof(ClientHandlers.UpdateClientUrls))
             .WithSummary("Renews the list of client urls (redirect cors etc).")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ClientHandlers.GetClient))
             .WithParameterValidation<UpdateClientUrls>();

        group.MapPost("{clientId}/resources", ClientHandlers.AddClientResources)
             .WithName(nameof(ClientHandlers.AddClientResources))
             .WithSummary("Adds an identity resource to the specified client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ClientHandlers.GetClient));

        group.MapDelete("{clientId}/resources", ClientHandlers.DeleteClientResource)
             .WithName(nameof(ClientHandlers.DeleteClientResource))
             .WithSummary("Removes a range of identity resources from the specified client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ClientHandlers.GetClient));

        group.MapPost("{clientId}/grant-types/{grantType}", ClientHandlers.AddClientGrantType)
             .WithName(nameof(ClientHandlers.AddClientGrantType))
             .WithSummary("Adds a grant type to the specified client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ClientHandlers.GetClient));

        group.MapDelete("{clientId}/grant-types/{grantType}", ClientHandlers.DeleteClientGrantType)
             .WithName(nameof(ClientHandlers.DeleteClientGrantType))
             .WithSummary("Removes a grant type from the specified client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ClientHandlers.GetClient));

        group.MapPost("{clientId}/secrets", ClientHandlers.AddClientSecret)
             .WithName(nameof(ClientHandlers.AddClientSecret))
             .WithSummary("Adds a new secret to an existing client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ClientHandlers.GetClient))
             .WithParameterValidation<CreateSecretRequest>();

        group.MapPost("{clientId}/certificates", ClientHandlers.UploadCertificate)
             .WithName(nameof(ClientHandlers.UploadCertificate))
             .WithSummary("Adds a new secret, from a certificate, to an existing client.")
             .Accepts<CertificateUploadRequest>("multipart/form-data")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .WithParameterValidation<CertificateUploadRequest>()
             .LimitUpload(2097152, ".cer, .pfx"); // 2 MegaBytes

        group.MapPost("certificates", ClientHandlers.GetCertificateMetadata)
             .WithName(nameof(ClientHandlers.GetCertificateMetadata))
             .WithSummary("Gets the metadata of a certificate for display.")
             .Accepts<CertificateUploadRequest>("multipart/form-data")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader)
             .WithParameterValidation<CertificateUploadRequest>()
             .LimitUpload(2097152, ".cer, .pfx");// 2 MegaBytes

        group.MapGet("{clientId}/certificates/{secretId:int}", ClientHandlers.GetCertificate)
             .WithName(nameof(ClientHandlers.GetCertificate))
             .WithSummary("Downloads a client secret if it is a certificate.")
             .Produces<Stream>(StatusCodes.Status200OK, "application/x-x509-user-cert")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader)
             .InvalidateCache(nameof(ClientHandlers.GetClient));

        group.MapDelete("{clientId}/secrets/{secretId}", ClientHandlers.DeleteClientSecret)
             .WithName(nameof(ClientHandlers.DeleteClientSecret))
             .WithSummary("Removes a specified secret from a client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ClientHandlers.GetClient));

        group.MapDelete("{clientId}", ClientHandlers.DeleteClient)
             .WithName(nameof(ClientHandlers.DeleteClient))
             .WithSummary("Permanently deletes an existing client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter);

        group.MapGet("{clientId}/theme", ClientHandlers.GetClientTheme)
             .WithName(nameof(ClientHandlers.GetClientTheme))
             .WithSummary("Gets the UI configuration for the specified client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader);

        group.MapPut("{clientId}/theme", ClientHandlers.CreateOrUpdateClientTheme)
             .WithName(nameof(ClientHandlers.CreateOrUpdateClientTheme))
             .WithSummary("Creates or updates the ui configuration for the specified client.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .WithParameterValidation<ClientThemeConfigRequest>();

        return group;
    }
}
