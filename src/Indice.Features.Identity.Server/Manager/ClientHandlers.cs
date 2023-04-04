using System.Security.Cryptography.X509Certificates;
using Humanizer;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Identity.Core;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using IdentityServer4.Extensions;
using Indice.Services;
using Indice.Features.Identity.Core.Events;
using Client = IdentityServer4.EntityFramework.Entities.Client;
using ClientClaim = IdentityServer4.EntityFramework.Entities.ClientClaim;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Indice.Features.Identity.Core.Extensions;
using System.Text.Json;
using Indice.Features.Identity.Core.Models;
using Indice.Serialization;

namespace Indice.Features.Identity.Server.Manager;
internal static class ClientHandlers
{

    internal static async Task<Ok<ResultSet<ClientInfo>>> GetClients(ExtendedConfigurationDbContext configurationDbContext, [AsParameters]ListOptions options) {

        var query = configurationDbContext.Clients.AsQueryable();
        if (!string.IsNullOrEmpty(options.Search)) {
            var searchTerm = options.Search.ToLower();
            query = query.Where(x => x.ClientId.ToLower().Contains(searchTerm) || x.ClientName.Contains(searchTerm));
        }
        var clients = await query.Select(x => new ClientInfo {
            ClientId = x.ClientId,
            ClientName = x.ClientName,
            ClientUri = x.ClientUri,
            LogoUri = x.LogoUri,
            Description = x.Description,
            AllowRememberConsent = x.AllowRememberConsent,
            Enabled = x.Enabled,
            RequireConsent = x.RequireConsent,
            NonEditable = x.NonEditable
        })
        .ToResultSetAsync(options);
        return TypedResults.Ok(clients);
    }

    internal static async Task<Results<Ok<SingleClientInfo>, NotFound>> GetClient(ExtendedConfigurationDbContext configurationDbContext, string clientId) {
        var client = await configurationDbContext
            .Clients
            .Include(x => x.Properties)
            .AsNoTracking()
            .Select(x => new SingleClientInfo {
                ClientId = x.ClientId,
                ClientName = x.ClientName,
                ClientUri = x.ClientUri,
                LogoUri = x.LogoUri,
                Description = x.Description,
                AllowRememberConsent = x.AllowRememberConsent,
                Enabled = x.Enabled,
                RequireConsent = x.RequireConsent,
                AllowedCorsOrigins = x.AllowedCorsOrigins.Select(x => x.Origin),
                PostLogoutRedirectUris = x.PostLogoutRedirectUris.Select(x => x.PostLogoutRedirectUri),
                RedirectUris = x.RedirectUris.Select(x => x.RedirectUri),
                IdentityTokenLifetime = x.IdentityTokenLifetime,
                AccessTokenLifetime = x.AccessTokenLifetime,
                ConsentLifetime = x.ConsentLifetime,
                UserSsoLifetime = x.UserSsoLifetime,
                FrontChannelLogoutUri = x.FrontChannelLogoutUri,
                PairWiseSubjectSalt = x.PairWiseSubjectSalt,
                AccessTokenType = (IdentityServer4.Models.AccessTokenType)x.AccessTokenType,
                FrontChannelLogoutSessionRequired = x.FrontChannelLogoutSessionRequired,
                IncludeJwtId = x.IncludeJwtId,
                AllowAccessTokensViaBrowser = x.AllowAccessTokensViaBrowser,
                AlwaysIncludeUserClaimsInIdToken = x.AlwaysIncludeUserClaimsInIdToken,
                AlwaysSendClientClaims = x.AlwaysSendClientClaims,
                AuthorizationCodeLifetime = x.AuthorizationCodeLifetime,
                RequirePkce = x.RequirePkce,
                AllowPlainTextPkce = x.AllowPlainTextPkce,
                ClientClaimsPrefix = x.ClientClaimsPrefix,
                GrantTypes = x.AllowedGrantTypes.Select(x => x.GrantType),
                AbsoluteRefreshTokenLifetime = x.AbsoluteRefreshTokenLifetime,
                AllowOfflineAccess = x.AllowOfflineAccess,
                NonEditable = x.NonEditable,
                RefreshTokenExpiration = (IdentityServer4.Models.TokenExpiration)x.RefreshTokenExpiration,
                RefreshTokenUsage = (IdentityServer4.Models.TokenUsage)x.RefreshTokenUsage,
                UpdateAccessTokenClaimsOnRefresh = x.UpdateAccessTokenClaimsOnRefresh,
                BackChannelLogoutUri = x.BackChannelLogoutUri,
                BackChannelLogoutSessionRequired = x.BackChannelLogoutSessionRequired,
                UserCodeType = x.UserCodeType,
                DeviceCodeLifetime = x.DeviceCodeLifetime,
                SlidingRefreshTokenLifetime = x.SlidingRefreshTokenLifetime,
                EnableLocalLogin = x.EnableLocalLogin,
                IdentityProviderRestrictions = x.IdentityProviderRestrictions.Select(x => x.Provider),
                ApiResources = x.AllowedScopes.Join(
                    configurationDbContext.ApiResources.SelectMany(x => x.Scopes),
                    clientScope => clientScope.Scope,
                    apiScope => apiScope.Scope,
                    (clientScope, apiScope) => apiScope.Scope
                )
                .Select(x => x),
                IdentityResources = x.AllowedScopes.Join(
                    configurationDbContext.IdentityResources,
                    clientScope => clientScope.Scope,
                    identityResource => identityResource.Name,
                    (clientScope, identityResource) => identityResource.Name
                )
                .Select(x => x),
                Claims = x.Claims.Select(x => new ClaimInfo {
                    Id = x.Id,
                    Type = x.Type,
                    Value = x.Value
                }),
                Secrets = x.ClientSecrets.Select(x => new ClientSecretInfo {
                    Id = x.Id,
                    Type = x.Type,
                    Value = GetClientSecretValue(x),
                    Description = x.Description,
                    Expiration = x.Expiration
                }),
                Translations = TranslationDictionary<ClientTranslation>.FromJson(x.Properties.Any(clientProperty => clientProperty.Key == ClientPropertyKeys.Translation)
                    ? x.Properties.Single(clientProperty => clientProperty.Key == ClientPropertyKeys.Translation).Value
                    : string.Empty)
            })
            .SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(client);
    }

    internal static async Task<Results<CreatedAtRoute<ClientInfo>, ValidationProblem>> CreateClient(
        ExtendedConfigurationDbContext configurationDbContext, 
        IPlatformEventService eventService,
        IConfiguration configuration,
        ClaimsPrincipal currentUser,
        CreateClientRequest request) {
        if (request is null) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>() { [""] = new[] { "Request body cannot be null." } });
        }
        var clientIdExists = (await configurationDbContext.Clients.CountAsync(x => x.ClientId == request.ClientId)) > 0;
        if (clientIdExists) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>() { [nameof(request.ClientId).Camelize()] = new[] { $"Client with id '{request.ClientId}' already exists." } });
        }
        var client = CreateForType(request.ClientType, configuration.GetAuthority(), request);
        configurationDbContext.Clients.Add(client);
        configurationDbContext.ClientUsers.Add(new ClientUser {
            Client = client,
            UserId = currentUser.GetSubjectId()
        });
        await configurationDbContext.SaveChangesAsync();
        await eventService.Publish(new ClientCreatedEvent(client.ToModel()));
        var response = ClientInfo.FromClient(client);
        return TypedResults.CreatedAtRoute(response, nameof(GetClient), new { clientId = client.ClientId });
    }

    internal static async Task<Results<NoContent, NotFound>> UpdateClient(ExtendedConfigurationDbContext configurationDbContext, string clientId, UpdateClientRequest request) {
        var client = await configurationDbContext.Clients.Include(x => x.Properties).Include(x => x.IdentityProviderRestrictions).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        client.AbsoluteRefreshTokenLifetime = request.AbsoluteRefreshTokenLifetime;
        client.AccessTokenLifetime = request.AccessTokenLifetime;
        client.AccessTokenType = (int)request.AccessTokenType;
        client.AllowAccessTokensViaBrowser = request.AllowAccessTokensViaBrowser;
        client.AllowOfflineAccess = request.AllowOfflineAccess;
        client.AllowPlainTextPkce = request.AllowPlainTextPkce;
        client.AllowRememberConsent = request.AllowRememberConsent;
        client.AlwaysIncludeUserClaimsInIdToken = request.AlwaysIncludeUserClaimsInIdToken;
        client.AlwaysSendClientClaims = request.AlwaysSendClientClaims;
        client.AuthorizationCodeLifetime = request.AuthorizationCodeLifetime;
        client.BackChannelLogoutSessionRequired = request.BackChannelLogoutSessionRequired;
        client.BackChannelLogoutUri = request.BackChannelLogoutUri;
        client.ClientClaimsPrefix = request.ClientClaimsPrefix;
        client.ClientName = request.ClientName;
        client.ClientUri = request.ClientUri;
        client.ConsentLifetime = request.ConsentLifetime;
        client.Description = request.Description;
        client.DeviceCodeLifetime = request.DeviceCodeLifetime;
        client.Enabled = request.Enabled;
        client.FrontChannelLogoutSessionRequired = request.FrontChannelLogoutSessionRequired;
        client.FrontChannelLogoutUri = request.FrontChannelLogoutUri;
        client.IdentityTokenLifetime = request.IdentityTokenLifetime;
        client.IncludeJwtId = request.IncludeJwtId;
        client.LogoUri = request.LogoUri;
        client.PairWiseSubjectSalt = request.PairWiseSubjectSalt;
        client.RefreshTokenExpiration = (int)request.RefreshTokenExpiration;
        client.RefreshTokenUsage = (int)request.RefreshTokenUsage;
        client.RequireConsent = request.RequireConsent;
        client.RequirePkce = request.RequirePkce;
        client.UpdateAccessTokenClaimsOnRefresh = request.UpdateAccessTokenClaimsOnRefresh;
        client.UserCodeType = request.UserCodeType;
        client.UserSsoLifetime = request.UserSsoLifetime;
        client.SlidingRefreshTokenLifetime = request.SlidingRefreshTokenLifetime;
        if (request.EnableLocalLogin.HasValue) {
            client.EnableLocalLogin = request.EnableLocalLogin.Value;
        }
        client.IdentityProviderRestrictions.RemoveAll(x => true);
        client.IdentityProviderRestrictions.AddRange(request.IdentityProviderRestrictions.Select(provider => new ClientIdPRestriction {
            Provider = provider,
            Client = client
        }));
        var clientTranslations = client.Properties?.SingleOrDefault(x => x.Key == ClientPropertyKeys.Translation);
        if (clientTranslations is null) {
            AddClientTranslations(client, request.Translations.ToJson());
        } else {
            clientTranslations.Value = request.Translations.ToJson() ?? string.Empty;
        }
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
    internal static async Task<Results<Ok<ClaimInfo>, NotFound, ValidationProblem>> AddClientClaim(ExtendedConfigurationDbContext configurationDbContext, string clientId, CreateClaimRequest request) {
        var client = await configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        var claimToAdd = new ClientClaim {
            Client = client,
            ClientId = client.Id,
            Type = request.Type,
            Value = request.Value
        };
        client.Claims = new List<ClientClaim> {
            claimToAdd
        };
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.Ok(new ClaimInfo {
            Id = claimToAdd.Id,
            Type = claimToAdd.Type,
            Value = claimToAdd.Value
        });
    }
    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteClientClaim(ExtendedConfigurationDbContext configurationDbContext, string clientId, int claimId) {
        var client = await configurationDbContext.Clients.Include(x => x.Claims).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        client.Claims ??= new List<ClientClaim>();
        var claimToRemove = client.Claims.SingleOrDefault(x => x.Id == claimId);
        if (claimToRemove == null) {
            return TypedResults.NotFound();
        }
        client.Claims.Remove(claimToRemove);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateClientUrls(ExtendedConfigurationDbContext configurationDbContext, string clientId, UpdateClientUrls request) {
        var client = await configurationDbContext
            .Clients
            .Include(x => x.AllowedCorsOrigins)
            .Include(x => x.RedirectUris)
            .Include(x => x.PostLogoutRedirectUris)
            .SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        client.AllowedCorsOrigins?.RemoveAll(x => true);
        client.RedirectUris?.RemoveAll(x => true);
        client.PostLogoutRedirectUris?.RemoveAll(x => true);
        if (request.AllowedCorsOrigins?.Count() > 0) {
            client.AllowedCorsOrigins.AddRange(request.AllowedCorsOrigins.Select(x => new ClientCorsOrigin {
                ClientId = client.Id,
                Origin = x.TrimEnd('/')
            }));
        }
        if (request.RedirectUris?.Count() > 0) {
            client.RedirectUris.AddRange(request.RedirectUris.Select(x => new ClientRedirectUri {
                ClientId = client.Id,
                RedirectUri = x
            }));
        }
        if (request.PostLogoutRedirectUris?.Count() > 0) {
            client.PostLogoutRedirectUris.AddRange(request.PostLogoutRedirectUris.Select(x => new ClientPostLogoutRedirectUri {
                ClientId = client.Id,
                PostLogoutRedirectUri = x
            }));
        }
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> AddClientResources(ExtendedConfigurationDbContext configurationDbContext, string clientId, string[] resources) {
        var client = await configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        resources ??= Array.Empty<string>();
        client.AllowedScopes = new List<ClientScope>();
        client.AllowedScopes.AddRange(resources.Select(x => new ClientScope {
            ClientId = client.Id,
            Scope = x
        }));
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteClientResource(ExtendedConfigurationDbContext configurationDbContext, string clientId, string[] resources) {
        var client = await configurationDbContext.Clients.Include(x => x.AllowedScopes).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        resources ??= Array.Empty<string>();
        client.AllowedScopes ??= new List<ClientScope>();
        var resourcesToRemove = client.AllowedScopes.Where(x => resources.Contains(x.Scope)).ToList();
        if (resourcesToRemove == null) {
            return TypedResults.NotFound();
        }
        foreach (var resource in resourcesToRemove) {
            client.AllowedScopes.Remove(resource);
        }
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<GrantTypeInfo>, NotFound, ValidationProblem>> AddClientGrantType(ExtendedConfigurationDbContext configurationDbContext, string clientId, string grantType) {
        var client = await configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        var grantTypeToAdd = new ClientGrantType {
            GrantType = grantType,
            ClientId = client.Id
        };
        client.AllowedGrantTypes = new List<ClientGrantType> {
            grantTypeToAdd
        };
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.Ok(new GrantTypeInfo {
            Id = grantTypeToAdd.Id,
            Name = grantTypeToAdd.GrantType
        });
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteClientGrantType(ExtendedConfigurationDbContext configurationDbContext, string clientId, string grantType) {
        var client = await configurationDbContext.Clients.Include(x => x.AllowedGrantTypes).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        client.AllowedGrantTypes ??= new List<ClientGrantType>();
        var grantTypeToRemove = client.AllowedGrantTypes.SingleOrDefault(x => x.GrantType == grantType);
        if (grantTypeToRemove == null) {
            return TypedResults.NotFound();
        }
        client.AllowedGrantTypes.Remove(grantTypeToRemove);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<SecretInfo>, NotFound, ValidationProblem>> AddClientSecret(ExtendedConfigurationDbContext configurationDbContext, string clientId, CreateSecretRequest request) {
        var client = await configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client is null) {
            return TypedResults.NotFound();
        }
        var newSecret = new ClientSecret {
            Description = request.Description,
            Value = request.Value.ToSha256(),
            Expiration = request.Expiration,
            Type = IdentityServerConstants.SecretTypes.SharedSecret,
            ClientId = client.Id
        };
        client.ClientSecrets = new List<ClientSecret> { newSecret };
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.Ok(new SecretInfo {
            Id = newSecret.Id,
            Description = newSecret.Description,
            Expiration = newSecret.Expiration,
            Type = newSecret.Type,
            Value = GetClientSecretValue(newSecret)
        });
    }

    internal static async Task<Results<Ok<SecretInfo>, NotFound, ValidationProblem>> UploadCertificate(ExtendedConfigurationDbContext configurationDbContext, string clientId, CertificateUploadRequest request) {
        if (request?.File == null) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = new[] { "Please upload a certificate." } });
        }
        var memoryStream = new MemoryStream();
        X509Certificate2 certificate;
        try {
            await request.File.CopyToAsync(memoryStream);
            var certificateBytes = memoryStream.ToArray();
            certificate = new X509Certificate2(certificateBytes, request.Password);
        } catch (CryptographicException) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = new[] { "Uploaded certificate is not valid." } });
        } finally {
            await memoryStream.DisposeAsync();
        }
        var client = await configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        var newSecret = new ClientSecret {
            Description = certificate.Subject,
            Value = Convert.ToBase64String(certificate.GetRawCertData()),
            Expiration = DateTime.Parse(certificate.GetExpirationDateString()),
            Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
            ClientId = client.Id
        };
        client.ClientSecrets = new List<ClientSecret> { newSecret };
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.Ok(new SecretInfo {
            Id = newSecret.Id,
            Description = newSecret.Description,
            Expiration = newSecret.Expiration,
            Type = newSecret.Type,
            Value = GetClientSecretValue(newSecret)
        });
    }
    internal static async Task<Results<Ok<SecretInfoBase>, NotFound, ValidationProblem>> GetCertificateMetadata(CertificateUploadRequest request) {
        if (request?.File == null) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = new[] { "Please upload a certificate." } });
        }
        var memoryStream = new MemoryStream();
        X509Certificate2 certificate;
        try {
            await request.File.CopyToAsync(memoryStream);
            var certificateBytes = memoryStream.ToArray();
            certificate = new X509Certificate2(certificateBytes, request.Password);
        } catch (CryptographicException) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = new[] { "Uploaded certificate is not valid." } });
        } finally {
            await memoryStream.DisposeAsync();
        }
        var secretInfo = new SecretInfoBase {
            Description = certificate.Subject,
            Value = $"Thumbprint: {certificate.Thumbprint}, Expiration Date: {certificate.GetExpirationDateString()}",
            Expiration = DateTime.Parse(certificate.GetExpirationDateString()),
            Type = IdentityServerConstants.SecretTypes.X509CertificateBase64
        };
        return TypedResults.Ok(secretInfo);
    }
    internal static async Task<Results<FileContentHttpResult, NotFound, ValidationProblem>> GetCertificate(ExtendedConfigurationDbContext configurationDbContext, string clientId, int secretId) {
        var client = await configurationDbContext.Clients.Include(x => x.ClientSecrets).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        var clientSecret = client.ClientSecrets.FirstOrDefault(clientSecret => clientSecret.Id == secretId);
        if (clientSecret == null) {
            return TypedResults.NotFound();
        }
        if (clientSecret.Type != IdentityServerConstants.SecretTypes.X509CertificateBase64) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["type"] = new[] { $"A secret of type {clientSecret.Type} cannot be downloaded." } });
        }
        X509Certificate2 certificate = null;
        byte[] certificateBytes;
        try {
            certificate = new X509Certificate2(Convert.FromBase64String(clientSecret.Value));
            certificateBytes = certificate.Export(X509ContentType.Cert);
        } catch (CryptographicException) {
            throw;
        } finally {
            certificate?.Dispose();
        }
        return TypedResults.File(certificateBytes, contentType:"application/x-x509-user-cert", fileDownloadName: $"{client.ClientId}.cer");
    }
    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteClientSecret(ExtendedConfigurationDbContext configurationDbContext, string clientId, int secretId) {
        var client = await configurationDbContext.Clients.Include(x => x.ClientSecrets).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        if (client.ClientSecrets == null) {
            client.ClientSecrets = new List<ClientSecret>();
        }
        var secretToRemove = client.ClientSecrets.SingleOrDefault(x => x.Id == secretId);
        if (secretToRemove == null) {
            return TypedResults.NotFound();
        }
        client.ClientSecrets.Remove(secretToRemove);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteClient(ExtendedConfigurationDbContext configurationDbContext, string clientId) {
        var client = await configurationDbContext.Clients.AsNoTracking().SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return TypedResults.NotFound();
        }
        configurationDbContext.Clients.Remove(client);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<ClientThemeConfigResponse>, ValidationProblem>> GetClientTheme(
        ExtendedConfigurationDbContext configurationDbContext, 
        ClientThemeConfigTypeResolver themeConfigResolver, 
        string clientId) {
        var client = await configurationDbContext
            .Clients
            .Include(x => x.Properties)
            .Where(x => x.ClientId == clientId)
            .SingleOrDefaultAsync();
        if (client is null) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(Client.Id).Camelize()] = new[] { "Requested client does not exist." } });
        }
        var themeConfig = client.Properties.Where(x => x.Key == ClientPropertyKeys.ThemeConfig).FirstOrDefault();
        return TypedResults.Ok(new ClientThemeConfigResponse {
            Schema = themeConfigResolver.Resolve().ToJsonSchema().AsJsonElement(),
            Data = themeConfig is not null ? JsonSerializer.Deserialize<DefaultClientThemeConfig>(themeConfig.Value, JsonSerializerOptionDefaults.GetDefaultSettings()) : null
        });
    }

    internal static async Task<Results<NoContent, ValidationProblem>> CreateOrUpdateClientTheme(
        ExtendedConfigurationDbContext configurationDbContext,
        string clientId, ClientThemeConfigRequest request) {
        var client = await configurationDbContext
            .Clients
            .Include(x => x.Properties)
            .Where(x => x.ClientId == clientId)
            .SingleOrDefaultAsync();
        if (client is null) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(Client.Id).Camelize()] = new[] { "Requested client does not exist." } });
        }
        var themeConfig = client.Properties.Where(x => x.Key == ClientPropertyKeys.ThemeConfig).FirstOrDefault();
        var themeConfigValue = JsonSerializer.Serialize(request, JsonSerializerOptionDefaults.GetDefaultSettings());
        if (themeConfig is null) {
            client.Properties.Add(new ClientProperty {
                Client = client,
                ClientId = client.Id,
                Key = ClientPropertyKeys.ThemeConfig,
                Value = themeConfigValue
            });
        } else {
            themeConfig.Value = themeConfigValue;
        }
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    private static string GetClientSecretValue(ClientSecret clientSecret) {
        switch (clientSecret.Type) {
            case IdentityServerConstants.SecretTypes.SharedSecret:
            case IdentityServerConstants.SecretTypes.JsonWebKey:
                return $"{clientSecret.Value.Substring(0, 3)}***********";
            case IdentityServerConstants.SecretTypes.X509CertificateBase64:
                var certificateData = Convert.FromBase64String(clientSecret.Value);
                using (var certificate = new X509Certificate2(certificateData)) {
                    return $"Thumbprint: {certificate.Thumbprint}, Expiration Date: {certificate.GetExpirationDateString()}";
                }
            case IdentityServerConstants.SecretTypes.X509CertificateName:
            case IdentityServerConstants.SecretTypes.X509CertificateThumbprint:
                return clientSecret.Value;
            default:
                throw new ArgumentOutOfRangeException(nameof(clientSecret.Type));
        }
    }

    /// <summary>Creates default client configuration based on <see cref="ClientType"/>.</summary>
    /// <param name="clientType">The type of the client.</param>
    /// <param name="authorityUri">The IdentityServer instance URI.</param>
    /// <param name="clientRequest">Client information provided by the user.</param>
    private static Client CreateForType(ClientType clientType, string authorityUri, CreateClientRequest clientRequest) {
        var client = new Client {
            ClientId = clientRequest.ClientId,
            ClientName = clientRequest.ClientName,
            Description = clientRequest.Description,
            ClientUri = clientRequest.ClientUri,
            LogoUri = clientRequest.LogoUri,
            RequireConsent = clientRequest.RequireConsent,
            BackChannelLogoutSessionRequired = true,
            AllowedScopes = clientRequest.IdentityResources.Union(clientRequest.ApiResources).Select(scope => new ClientScope { Scope = scope }).ToList(),
            EnableLocalLogin = true,
            Enabled = true
        };
        if (!string.IsNullOrEmpty(clientRequest.RedirectUri)) {
            client.RedirectUris = new List<ClientRedirectUri> {
                new ClientRedirectUri { RedirectUri = clientRequest.RedirectUri }
            };
        }
        if (!string.IsNullOrEmpty(clientRequest.PostLogoutRedirectUri)) {
            client.PostLogoutRedirectUris = new List<ClientPostLogoutRedirectUri> {
                new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = clientRequest.PostLogoutRedirectUri }
            };
        }
        if (clientRequest.Secrets.Any()) {
            client.ClientSecrets = clientRequest.Secrets.Select(x => new ClientSecret {
                Type = IdentityServerConstants.SecretTypes.SharedSecret,
                Description = x.Description,
                Expiration = x.Expiration,
                Value = x.Value.ToSha256()
            })
            .ToList();
        }
        if (clientRequest.Translations.Any()) {
            AddClientTranslations(client, clientRequest.Translations.ToJson());
        }
        switch (clientType) {
            case ClientType.SPA:
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.AuthorizationCode
                    }
                };
                client.RequirePkce = true;
                client.RequireClientSecret = false;
                client.AllowedCorsOrigins = new List<ClientCorsOrigin> {
                    new ClientCorsOrigin {
                        Origin = clientRequest.ClientUri ?? authorityUri
                    }
                };
                break;
            case ClientType.WebApp:
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.Hybrid
                    }
                };
                client.RequirePkce = true;
                break;
            case ClientType.Native:
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.AuthorizationCode
                    }
                };
                client.RequirePkce = true;
                client.RequireClientSecret = false;
                break;
            case ClientType.Machine:
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.ClientCredentials
                    }
                };
                client.RequireConsent = false;
                break;
            case ClientType.Device:
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.DeviceFlow
                    }
                };
                break;
            case ClientType.SPALegacy:
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.Implicit
                    }
                };
                client.RequirePkce = false;
                client.RequireClientSecret = false;
                client.AllowAccessTokensViaBrowser = true;
                client.AllowedCorsOrigins = new List<ClientCorsOrigin> {
                    new ClientCorsOrigin {
                        Origin = clientRequest.ClientUri ?? authorityUri
                    }
                };
                break;
            default:
                throw new ArgumentNullException(nameof(clientType), "Cannot determine the type of the client.");
        }
        return client;
    }

    /// <summary>Adds translations to a <see cref="Client"/>.</summary>
    /// <remarks>If the parameter translations is null, string.Empty will be persisted.</remarks>
    /// <param name="client">The <see cref="Client"/></param>
    /// <param name="translations">The JSON string with the translations</param>
    private static void AddClientTranslations(Client client, string translations) {
        client.Properties ??= new List<ClientProperty>();
        client.Properties.Add(new ClientProperty {
            Key = ClientPropertyKeys.Translation,
            Value = translations ?? string.Empty,
            Client = client
        });
    }
}
