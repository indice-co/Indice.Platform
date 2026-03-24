#if NET10_0_OR_GREATER
using System.Collections.Immutable;
using System.Net.Mime;
using System.Reflection.Metadata;
using System.Text.Json.Nodes;
using Humanizer;
using Indice.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

// useful resources
//https://www.youtube.com/watch?v=pkQdwbYPRP4
//https://github.com/mikekistler/aspnet-transformer-gallery

/// <summary>
/// Provides extension methods for configuring OpenAPI options, including security schemes and document metadata.
/// </summary>
/// <remarks>This static class contains methods to enhance OpenAPI documentation by adding security schemes such
/// as OAuth2,  OpenID Connect, Basic Authentication, JWT, and API Key authentication. It also includes methods for
/// populating  document metadata like contact and license information based on application configuration.  These
/// methods are designed to simplify the process of configuring OpenAPI options for APIs, ensuring that  security and
/// metadata settings are properly integrated into the OpenAPI specification.</remarks>
public static class OpenApiExtensions
{

    /// <summary>
    /// Adds a transformer to the OpenAPI document that populates the document's metadata, such as contact and license
    /// information, based on the application's configuration.
    /// </summary>
    /// <remarks>This method retrieves API settings from the application's configuration and uses them to
    /// populate the OpenAPI document's contact and license information. If the configuration does not specify these
    /// settings, the corresponding fields in the OpenAPI document will remain unset.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the transformer will be added. Cannot be <see
    /// langword="null"/>.</param>
    /// <param name="documentTitle">Specify the document title explicitly. If null the title will be populated from configuration.</param>
    /// <returns>The updated <see cref="OpenApiOptions"/> instance with the document transformer applied.</returns>
    public static OpenApiOptions AddDocumentInfo(this OpenApiOptions options, string? documentTitle = null) {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
            var apiSettings = configuration.GetApiSettings() ?? new ApiSettings();
            var license = apiSettings.License == null ? null : new OpenApiLicense { Name = apiSettings.License.Name, Url = new Uri(apiSettings.License.Url!) };
            var contact = apiSettings.Contact == null ? null : new OpenApiContact { Name = apiSettings.Contact.Name, Url = new Uri(apiSettings.Contact.Url!), Email = apiSettings.Contact.Email };
            document.Info.Contact = contact;
            document.Info.License = license;
            var title = documentTitle ?? apiSettings.FriendlyName;
            if (!string.IsNullOrWhiteSpace(title)) {
                document.Info.Title = title;
            }
            return Task.CompletedTask;
        });
        options.AddNullableTransformer();
        options.AddMappedTypeTransformer();
        options.AddFluentValidationTransformer();
        options.AddConventionsTransformer();
        options.AddCustomConverterTransformer();
        //options.AddDictionaryTransformer();
        //options.AddArrayTransformer();
        //options.AddEnumTransformer();
        options.AddEndpointSecurityRequirementsTransformer();
        options.AddDocumentTransformer<CanonicalDocumentTransformer>();
        options.AddExamplesTransformer();
        return options;
    }

    /// <summary>
    /// Maps the specified type to the provided OpenAPI schema.
    /// </summary>
    /// <remarks>This method associates the specified type <typeparamref name="T"/> with the given OpenAPI
    /// schema. It is typically used to customize the schema representation for a specific type in OpenAPI
    /// documentation.</remarks>
    /// <typeparam name="T">The type to be mapped to the OpenAPI schema.</typeparam>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to configure.</param>
    /// <param name="schema">The <see cref="OpenApiSchema"/> to which the type will be mapped. Cannot be <see langword="null"/>.</param>
    /// <returns>The same <see cref="OpenApiOptions"/> instance passed as the <paramref name="options"/> parameter, allowing for
    /// method chaining.</returns>
    public static OpenApiOptions MapType<T>(this OpenApiOptions options, OpenApiSchema schema) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(schema);
        MappedTypeTransformer.MapType<T>(schema);
        return options;
    }
    /// <summary>
    /// Adds a document transformer to sort the endpoints (paths) in the OpenAPI document alphabetically.
    /// </summary>
    /// <remarks>This method modifies the OpenAPI document by sorting its paths alphabetically based on their
    /// keys. It ensures that the paths in the document are ordered consistently, which can be useful for improving
    /// readability or ensuring deterministic output in scenarios where path order matters.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the transformer is added.</param>
    /// <returns>The updated <see cref="OpenApiOptions"/> instance with the sorting transformer applied.</returns>
    public static OpenApiOptions SortByPath(this OpenApiOptions options) =>
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            var paths = document.Paths.ToImmutableSortedDictionary();
            document.Paths.Clear();
            foreach (var item in paths) {
                document.Paths.Add(item.Key, item.Value);
            }
            return Task.CompletedTask;
        });


    /// <summary>
    /// Configures the OpenAPI generator to use the ASP.NET Core MVC action name as the operation ID for each API
    /// operation.
    /// </summary>
    /// <remarks>If an operation does not already have an operation ID, this method sets it to the
    /// corresponding MVC action name. This can help ensure consistent and predictable operation IDs in generated
    /// OpenAPI documents.</remarks>
    /// <param name="options">The OpenApiOptions instance to configure. Cannot be null.</param>
    /// <returns>The same OpenApiOptions instance, enabling method chaining.</returns>
    public static OpenApiOptions ControllerActionAsOperationId(this OpenApiOptions options) =>
        options.AddOperationTransformer((operation, context, cancellationToken) => {
            var actionDescriptor = context.Description.ActionDescriptor;
            if (actionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerAction && string.IsNullOrWhiteSpace(operation.OperationId)) {
                operation.OperationId = $"{controllerAction.ControllerName}_{controllerAction.ActionName}";
                operation.Summary ??= controllerAction.ActionName.Humanize();
            }
            return Task.CompletedTask;
        });

    /// <summary>
    /// Configures the OpenAPI options to use the OAuth2 Authorization Code flow for authentication.
    /// </summary>
    /// <remarks>This method adds an OAuth2 Authorization Code flow to the OpenAPI security schemes, enabling
    /// integration with an Identity Server for token-based authentication. It sets up the token, refresh, and
    /// authorization URLs based on the application's configuration and defines the scopes required for the API.  If a
    /// security scheme with the specified <paramref name="schemeId"/> already exists, its Authorization Code flow is
    /// updated. Otherwise, a new security scheme is added.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="schemeId">The name of the security scheme to use for OAuth2. Defaults to <see langword="oauth2"/>.</param>
    /// <returns>The configured <see cref="OpenApiOptions"/> instance.</returns>
    public static OpenApiOptions AddOAuth2AuthorizationCodeFlow(this OpenApiOptions options, string schemeId = "oauth2") {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
            var apiSettings = configuration.GetApiSettings() ?? new ApiSettings();
            var authorizationCodeFlow = new OpenApiOAuthFlow {
                TokenUrl = new Uri(configuration.GetAuthority() + "/connect/token"),
                RefreshUrl = new Uri(configuration.GetAuthority() + "/connect/token"),
                AuthorizationUrl = new Uri(configuration.GetAuthority() + "/connect/authorize"),
                Scopes = GetScopes(apiSettings)
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            if (document.Components.SecuritySchemes.TryGetValue(schemeId, out var oauth2)) {
                oauth2.Flows!.AuthorizationCode = authorizationCodeFlow;
                return Task.CompletedTask;
            }
            document.Components.SecuritySchemes.Add(schemeId, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OAuth2,
                Description = "Identity Server OAuth2",
                Flows = new OpenApiOAuthFlows {
                    AuthorizationCode = authorizationCodeFlow
                }
            });
            //document.SecurityRequirements.Add(new OpenApiSecurityRequirement {
            //    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = schemeId } }] = GetScopes(apiSettings).Keys.ToList()
            //}); 

            return Task.CompletedTask;
        });
        options.AddSecurityRequirements(schemeId);
        return options;
    }

    /// <summary>
    /// Configures client credentials authentication for the OpenAPI specification.
    /// </summary>
    /// <remarks>This method adds a client credentials flow to the OpenAPI security schemes, enabling OAuth2
    /// authentication. If a security scheme with the specified <paramref name="schemeId"/> already exists, the client
    /// credentials flow is added to it. Otherwise, a new security scheme is created with the client credentials flow. 
    /// The method retrieves configuration settings from the application's <see cref="IConfiguration"/> service,
    /// including the authority URL and API scopes, to populate the client credentials flow.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="schemeId">The name of the security scheme to use for client credentials authentication. Defaults to <see
    /// langword="oauth2"/>.</param>
    /// <returns>The configured <see cref="OpenApiOptions"/> instance.</returns>
    public static OpenApiOptions AddClientCredentials(this OpenApiOptions options, string schemeId = "oauth2") {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
            var apiSettings = configuration.GetApiSettings() ?? new ApiSettings();
            var clientCredentialsFlow = new OpenApiOAuthFlow {
                TokenUrl = new Uri(configuration.GetAuthority() + "/connect/token"),
                Scopes = GetScopes(apiSettings)
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            if (document.Components.SecuritySchemes.TryGetValue(schemeId, out var oauth2)) {
                oauth2.Flows!.ClientCredentials = clientCredentialsFlow;
                return Task.CompletedTask;
            }
            document.Components.SecuritySchemes.Add(schemeId, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OAuth2,
                Description = "Identity Server OAuth2",
                Flows = new OpenApiOAuthFlows {
                    ClientCredentials = clientCredentialsFlow
                }
            });
            return Task.CompletedTask;
        });
        options.AddSecurityRequirements(schemeId);
        return options;
    }

    /// <summary>
    /// Adds an OpenID Connect security scheme to the OpenAPI documentation.
    /// </summary>
    /// <remarks>This method configures the OpenAPI documentation to include an OpenID Connect security scheme
    /// using the specified <paramref name="schemeId"/>. If the scheme already exists, no changes are made. The method
    /// retrieves configuration settings from the application's <see cref="IConfiguration"/> service to determine the
    /// authority metadata URL and API settings.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to configure.</param>
    /// <param name="schemeId">The identifier for the OpenID Connect security scheme. Defaults to <see langword="openid"/>.</param>
    /// <returns>The updated <see cref="OpenApiOptions"/> instance with the OpenID Connect security scheme added.</returns>
    public static OpenApiOptions AddOpenIdConnect(this OpenApiOptions options, string schemeId = "openid") {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
            var apiSettings = configuration.GetApiSettings() ?? new ApiSettings();
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            if (document.Components.SecuritySchemes.TryGetValue(schemeId, out var oidc)) {
                return Task.CompletedTask;
            }
            document.Components.SecuritySchemes.Add(schemeId, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OpenIdConnect,
                Description = "Identity Server Openid connect",
                OpenIdConnectUrl = new Uri(configuration.GetAuthorityMetadata()!)
            });
            return Task.CompletedTask;
        });
        options.AddSecurityRequirements(schemeId);
        return options;
    }

    /// <summary>
    /// Adds Basic Authentication support to the OpenAPI specification.
    /// </summary>
    /// <remarks>This method configures the OpenAPI specification to include a Basic Authentication scheme.
    /// The scheme requires clients to provide a username and password via the `Authorization` header. If the specified
    /// scheme name already exists in the OpenAPI document's security schemes, the method will not overwrite
    /// it.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="schemeId">The name of the authentication scheme to use. Defaults to <see langword="basic"/>. If a scheme with the
    /// specified name already exists, no changes will be made.</param>
    /// <returns>The modified <see cref="OpenApiOptions"/> instance, allowing for method chaining.</returns>
    public static OpenApiOptions AddBasicAuthentication(this OpenApiOptions options, string schemeId = "basic") {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            if (document.Components.SecuritySchemes.TryGetValue(schemeId, out var basicAuth)) {
                return Task.CompletedTask;
            }
            document.Components.SecuritySchemes.Add(schemeId, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                Description = "Input your username and password to access this API",
                Name = "Authorization",
                In = ParameterLocation.Header
            });
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement {
                [new(schemeId, document)] = []
            });
            return Task.CompletedTask;
        });
        return options;
    }

    /// <summary>
    /// Configures the OpenAPI options to include an OAuth2 implicit flow security scheme.
    /// </summary>
    /// <remarks>This method adds an OAuth2 implicit flow security scheme to the OpenAPI document. The
    /// implicit flow is configured using the application's authority URL and API settings, and includes token, refresh,
    /// and authorization URLs, as well as the defined scopes.  If a security scheme with the specified <paramref
    /// name="schemeId"/> already exists, its implicit flow is updated. Otherwise, a new security scheme is added to the
    /// OpenAPI document.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="schemeId">The identifier for the OAuth2 security scheme. Defaults to "oauth2" if not specified.</param>
    /// <returns>The configured <see cref="OpenApiOptions"/> instance.</returns>
    public static OpenApiOptions AddOAuth2ImplicitFlow(this OpenApiOptions options, string schemeId = "oauth2") {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
            var apiSettings = configuration.GetApiSettings() ?? new ApiSettings();
            var implicitFlow = new OpenApiOAuthFlow {
                TokenUrl = new Uri(configuration.GetAuthority() + "/connect/token"),
                RefreshUrl = new Uri(configuration.GetAuthority() + "/connect/token"),
                AuthorizationUrl = new Uri(configuration.GetAuthority() + "/connect/authorize"),
                Scopes = GetScopes(apiSettings)
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            if (document.Components.SecuritySchemes.TryGetValue(schemeId, out var oauth2)) {
                oauth2.Flows!.Implicit = implicitFlow;
                return Task.CompletedTask;
            }
            document.Components.SecuritySchemes.Add(schemeId, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OAuth2,
                Description = "Identity Server OAuth2",
                Flows = new OpenApiOAuthFlows {
                    Implicit = implicitFlow
                }
            });
            return Task.CompletedTask;
        });
        options.AddSecurityRequirements(schemeId);
        return options;
    }

    /// <summary>
    /// Adds a JWT security scheme to the OpenAPI options, enabling JWT-based authentication for the API.
    /// </summary>
    /// <remarks>This method configures the OpenAPI documentation to include a JWT security scheme, allowing
    /// clients  to authenticate using a bearer token. The scheme is added to the OpenAPI components if it does not 
    /// already exist.    The security scheme is defined with the following properties:  - Type: HTTP  - Scheme: Bearer 
    /// - BearerFormat: JWT  - Description: "Input your JWT token to access this API"  - Name: Authorization  - In:
    /// Header    If the specified <paramref name="schemeId"/> already exists in the OpenAPI components, no changes are
    /// made.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the JWT security scheme will be added.</param>
    /// <param name="schemeId">The identifier for the JWT security scheme. Defaults to <see langword="jwt"/>.</param>
    /// <returns>The modified <see cref="OpenApiOptions"/> instance with the JWT security scheme added.</returns>
    public static OpenApiOptions AddJwt(this OpenApiOptions options, string schemeId = "jwt") {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            if (document.Components.SecuritySchemes.TryGetValue(schemeId, out var jwt)) {
                return Task.CompletedTask;
            }
            document.Components.SecuritySchemes.Add(schemeId, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Input your JWT token to access this API",
                Name = "Authorization",
                In = ParameterLocation.Header
            });
            return Task.CompletedTask;
        });
        options.AddSecurityRequirements(schemeId);
        return options;
    }

    /// <summary>
    /// Adds an API key security scheme to the OpenAPI document.
    /// </summary>
    /// <remarks>This method adds an API key security scheme to the OpenAPI document if it does not already
    /// exist. The scheme is configured to use the <c>X-API-KEY</c> header for authentication.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="schemeId">The unique identifier for the API key security scheme. Defaults to <see langword="ApiKey"/>.</param>
    /// <param name="schemeName">The name of the API key scheme. Defaults to <see langword="Token"/>.</param>
    /// <returns>The modified <see cref="OpenApiOptions"/> instance with the API key security scheme added.</returns>
    public static OpenApiOptions AddApiKey(this OpenApiOptions options, string schemeId = "ApiKey", string schemeName = "Token") {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            if (document.Components.SecuritySchemes.TryGetValue(schemeId, out var apiKey)) {
                return Task.CompletedTask;
            }
            document.Components.SecuritySchemes.Add(schemeId, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.ApiKey,
                Scheme = schemeName,
                In = ParameterLocation.Header,
                Name = "X-API-KEY",
                Description = "Input your API key to access this API"
            });
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement {
                [new (schemeId, document)] = []
            });
            return Task.CompletedTask;
        });
        return options;
    }

    /// <summary>
    /// Adds extra header parameters to the OpenAPI operation descriptions based on metadata.
    /// </summary>
    /// <remarks>This method scans the endpoint metadata for instances of <see
    /// cref="ExtraHeaderParameterMetadata"/> and adds corresponding header parameters to the OpenAPI operation
    /// descriptions. Each header parameter is optional and includes its name, description, and location in the request. 
    /// An example for that would be the Requires Totp endpoint filter
    /// headers.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the header parameters will be added.</param>
    /// <returns>The updated <see cref="OpenApiOptions"/> instance with the extra header parameters configured.</returns>
    public static OpenApiOptions AddExtraHeaderParameters(this OpenApiOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        options.AddOperationTransformer((operation, context, cancellationToken) => {
            var extraHeaderParams = context.Description.ActionDescriptor.EndpointMetadata.OfType<ExtraHeaderParameterMetadata>();
            foreach (var item in extraHeaderParams) {
                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter {
                    Name = item.HeaderName,
                    In = ParameterLocation.Header,
                    Description = item.Description,
                    Required = item.Required,
                    Schema = new OpenApiSchema() { Type = JsonSchemaType.String }
                });
            }
            return Task.CompletedTask;
        });
        return options;
    }

    private static OpenApiOptions AddSecurityRequirements(this OpenApiOptions options, string schemeId) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemeId);

        options.AddOperationTransformer((operation, context, cancellationToken) => {
            if ((operation.Security is null || operation.Security.Count == 0) &&
                context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any() &&
                !context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()) {
                var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
                var apiSettings = configuration.GetApiSettings() ?? new ApiSettings();
                var scopes = schemeId == "oauth2" ? GetScopes(apiSettings).Keys.ToList() : [];
                operation.Security = [
                    new OpenApiSecurityRequirement { [new (schemeId)] = scopes }
                ];
            }
            return Task.CompletedTask;
        });
        return options;
    }

    private static Dictionary<string, string> GetScopes(ApiSettings? settings) {
        settings ??= new ApiSettings();
        // Define the OAuth2.0 scheme that's in use (i.e. Implicit Flow).
        var scopes = new Dictionary<string, string> {
            [settings.ResourceName] = $"Access to {settings.FriendlyName}",
        };
        foreach (var scope in settings.Scopes) {
            scopes.Add(scope.Name, scope.Description ?? scope.Name);
        }
        return scopes;
    }
}

/// <summary>
/// Represents endpoint metadata indicating an OpenApi endpoint requires an extra header to be displayed.
/// </summary>
/// <remarks>This will be used to expose a header.</remarks>
public record ExtraHeaderParameterMetadata(string HeaderName, bool Required, string? Description = null);


/// <summary>
/// Represents metadata for an example associated with an OpenAPI operation request body, including its name, value, and an optional
/// description.
/// </summary>
/// <remarks>Use this record to supply example data for OpenAPI operations, such as for documentation or client
/// generation purposes. The example value should conform to the expected schema of the associated operation.</remarks>
/// <param name="ExampleName">The unique name identifying the example. Cannot be null or empty.</param>
/// <param name="Value">The example value represented as an OpenAPI-compatible object. Cannot be null.</param>
/// <param name="ExternalValue">An optional URL pointing to an external resource containing the example. May be null.</param>
/// <param name="Summary">An optional brief summary of the example. May be null.</param>
/// <param name="Description">An optional description providing additional context or details about the example. May be null.</param>
/// <param name="ContentType">The content type of the example. Defaults to 'application/json'.</param>
public record EndpointBodyExampleMetadata(string ExampleName, JsonNode? Value, string? ExternalValue = null, string? Summary = null, string? Description = null, string ContentType = MediaTypeNames.Application.Json);

#endif