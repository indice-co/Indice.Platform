using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hellang.Middleware.ProblemDetails;
using IdentityModel;
using Indice.AspNetCore.Middleware;
using Indice.AspNetCore.Swagger;
using Indice.Configuration;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Worker.Azure;
using Indice.Features.Multitenancy.Core;
using Indice.MultitenantApi.Data;
using Indice.MultitenantApi.Models;
using Indice.MultitenantApi.Services;
using Indice.MultitenantApi.Swagger.Filters;
using Indice.Types;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Indice.MultitenantApi
{
    internal static class HostingExtensions
    {
        public static WebApplication ConfigureServices(this WebApplicationBuilder builder) {
            var services = builder.Services;
            var configuration = builder.Configuration;
            var hostingEnvironment = builder.Environment;
            var generalSettings = new GeneralSettings();
            builder.Configuration.Bind(GeneralSettings.Name, generalSettings);
            services.AddControllers()
                    .AddMessageEndpoints(options => {
                        options.ApiPrefix = "api";
                        options.DatabaseSchema = "msg";
                        options.RequiredScope = $"backoffice:{MessagesApi.Scope}";
                        options.UserClaimType = JwtClaimTypes.Subject;
                        options.ConfigureDbContext = (serviceProvider, builder) => {
                            if (hostingEnvironment.IsDevelopment()) {
                                builder.EnableDetailedErrors();
                                builder.EnableSensitiveDataLogging();
                            }
                            var tenant = serviceProvider.GetRequiredService<ITenantAccessor<ExtendedTenant>>().Tenant;
                            builder.UseSqlServer(tenant?.ConnectionString ?? configuration.GetConnectionString("MultitenantApiDb"));
                        };
                        options.UseMultiTenancy(accessLevel: (int)MemberAccessLevel.Owner);
                        options.UseFilesAzure();
                        options.UseEventDispatcherAzure((serviceProvider, eventDispatcherOptions) => {
                            var tenant = serviceProvider.GetRequiredService<ITenantAccessor<ExtendedTenant>>().Tenant;
                            eventDispatcherOptions.TenantIdSelector = () => tenant?.Id;
                        });
                        options.UseIdentityContactResolver(resolverOptions => {
                            resolverOptions.BaseAddress = new Uri(configuration["IdentityServer:BaseAddress"]);
                            resolverOptions.ClientId = configuration["IdentityServer:ClientId"];
                            resolverOptions.ClientSecret = configuration["IdentityServer:ClientSecret"];
                        });
                    })
                    .AddJsonOptions(options => {
                        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        options.JsonSerializerOptions.WriteIndented = true;
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    })
                    .AddAvatars();
            services.AddDbContext<SaasDbContext>(builder => {
                if (hostingEnvironment.IsDevelopment()) {
                    builder.EnableDetailedErrors();
                    builder.EnableSensitiveDataLogging();
                }
                builder.UseSqlServer(configuration.GetConnectionString("SaasDb"));
            })
            .AddProblemDetails(options => {
                options.IncludeExceptionDetails = (httpContext, exception) => hostingEnvironment.IsDevelopment();
                options.Map<BusinessException>(exception => {
                    var response = new ValidationProblemDetails(exception.Errors) {
                        Title = exception.Message,
                        Status = StatusCodes.Status400BadRequest
                    };
                    response.Extensions["code"] = exception.Code;
                    return response;
                });
                options.Map<NotImplementedException>(exception => new StatusCodeProblemDetails(StatusCodes.Status501NotImplemented));
                options.MapToStatusCode<HttpRequestException>(StatusCodes.Status503ServiceUnavailable);
                options.Map<Exception>(exception => new StatusCodeProblemDetails(StatusCodes.Status500InternalServerError));
            })
            .AddSwaggerGen(options => {
                options.IndiceDefaults(generalSettings);
                options.AddFluentValidationSupport();
                options.AddOAuth2AuthorizationCodeFlow(generalSettings);
                options.AddFormFileSupport();
                options.OperationFilter<TenantIdHeaderParameterOperationFilter>();
                options.IncludeXmlComments(Assembly.GetAssembly(typeof(Program)));
                options.AddDoc(MessagesApi.Scope, "Campaigns API", "API for managing campaigns in the backoffice tool.");
            })
            .AddScopeTransformation();
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => {
                options.Audience = generalSettings?.Api?.ResourceName;
                options.Authority = generalSettings?.Authority;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters.NameClaimType = JwtClaimTypes.Name;
                options.TokenValidationParameters.RoleClaimType = JwtClaimTypes.Role;
                options.ForwardDefaultSelector = BearerSelector.ForwardReferenceToken("Introspection");
            })
            .AddOAuth2Introspection("Introspection", options => {
                options.Authority = generalSettings?.Authority;
                options.CacheDuration = TimeSpan.FromMinutes(5);
                options.ClientId = generalSettings?.Api?.ResourceName;
                options.ClientSecret = generalSettings?.Api?.Secrets["Introspection"];
                options.EnableCaching = true;
            });
            services.AddMultiTenancy<ExtendedTenant>()
                    .FromHeader()
                    .FromRoute()
                    .FromHost()
                    .WithStore<SaasTenantStore>();
            return builder.Build();
        }

        public static WebApplication ConfigurePipeline(this WebApplication app) {
            var configuration = app.Configuration;
            var hostingEnvironment = app.Environment;
            var generalSettings = new GeneralSettings();
            app.Configuration.Bind(GeneralSettings.Name, generalSettings);
            if (hostingEnvironment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                if (configuration.UseRedirectToHost()) {
                    var rewrite = new RewriteOptions();
                    rewrite.Rules.Add(new RedirectToHostRewriteRule(generalSettings.Host));
                    app.UseRewriter(rewrite);
                }
                if (configuration.UseHttpsRedirection()) {
                    app.UseHttpsRedirection();
                }
                if (configuration.HstsEnabled()) {
                    app.UseHsts();
                }
            }
            app.UseRouting();
            app.UseResponseCaching();
            app.UseProblemDetails();
            app.UseMultiTenancy<ExtendedTenant>();
            app.UseAuthentication();
            app.UseAuthorization();
            if (hostingEnvironment.IsDevelopment() || generalSettings.EnableSwagger) {
                app.UseSwaggerUI(options => {
                    options.RoutePrefix = "docs";
                    options.SwaggerEndpoint($"/swagger/{MessagesApi.Scope}/swagger.json", MessagesApi.Scope);
                    options.OAuth2RedirectUrl($"{generalSettings.Host}/docs/oauth2-redirect.html");
                    options.OAuthClientId("swagger-ui");
                    options.OAuthAppName("Swagger UI");
                    options.DocExpansion(DocExpansion.None);
                    options.OAuthUsePkce();
                    options.OAuthScopeSeparator(" ");
                });
            }
            app.UseEndpoints(endpoints => {
                endpoints.MapSwagger();
                endpoints.MapControllers();
            });
            return app;
        }
    }
}
