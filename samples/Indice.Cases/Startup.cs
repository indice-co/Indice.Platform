using IdentityModel;
using Indice.Cases.Configuation;
using Indice.Configuration;
using Indice.Features.Cases;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Indice.Cases;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment) {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        HostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        Settings = Configuration.GetSection(GeneralSettings.Name).Get<GeneralSettings>();
    }

    public IConfiguration Configuration { get; }
    public GeneralSettings Settings { get; }
    public IWebHostEnvironment HostingEnvironment { get; }

    public void ConfigureServices(IServiceCollection services) {
        var generalSettings = Configuration.GetSection("General").Get<GeneralSettings>();
        var casesConnectionString = Configuration.GetConnectionString("CasesDb");

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddControllersWithViews()
                .AddCasesApiEndpoints(options => {
                    options.ApiPrefix = "api";
                    options.ConfigureDbContext = builder => builder.UseSqlServer(casesConnectionString);
                    options.DatabaseSchema = "case";
                    // options.ExpectedScope = $"backoffice:{CasesApiConstants.Scope}";
                    options.UserClaimType = JwtClaimTypes.Subject;
                    options.GroupIdClaimType = Configuration.GetSection(nameof(CasesApiOptions)).GetValue<string>("GroupIdClaimType");
                })
                .AddAdminCasesApiEndpoints(options => {
                    options.ApiPrefix = "api";
                    options.ConfigureDbContext = configBuilder => configBuilder.UseSqlServer(casesConnectionString);
                    options.DatabaseSchema = "case";
                    options.UserClaimType = JwtClaimTypes.Subject;
                    options.GroupIdClaimType = Configuration.GetSection(nameof(CasesApiOptions)).GetValue<string>("GroupIdClaimType");
                });

        services.AddCors(options => options.AddPolicy("AllowedOrigins", builder => {
            builder.WithOrigins(Configuration.GetSection("AllowedOrigins").Get<string[]>())
                .AllowAnyMethod()
                .AllowAnyHeader();
        }));

        services.AddAuthenticationConfig(generalSettings);
        services.AddAuthorizationConfig();

        services.AddWorkflow(Configuration, typeof(Startup).Assembly);
        services.AddElsaSwagger();

        services.AddSwaggerGenConfiguration(generalSettings);
    }

    public void Configure(IApplicationBuilder app) {
        var generalSettings = Configuration.GetSection("General").Get<GeneralSettings>();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseWorkflow(); // Register elsa http activities & dashboard

        app.UseEndpoints(endpoints => {
            endpoints.MapSwagger();
            endpoints.MapControllers();
            // For Dashboard.
            endpoints.MapFallbackToPage("/_Host");
        });

        // Configure the HTTP request pipeline.
        if (HostingEnvironment.IsDevelopment()) {
            app.UseDeveloperExceptionPage();

            app.UseSwagger();


            //var enableSwagger = env.IsDevelopment() || Configuration.GetValue<bool>($"{GeneralSettings.Name}:EnableSwagger");
            //if (enableSwagger)
            //{
            app.UseSwaggerUI(swaggerOptions => {
                swaggerOptions.RoutePrefix = "docs";
                swaggerOptions.OAuthClientId("swagger-ui");
                swaggerOptions.SwaggerEndpoint($"/swagger/{generalSettings.Api.ResourceName}/swagger.json", generalSettings.Api.ResourceName);
                swaggerOptions.SwaggerEndpoint($"/swagger/v1/swagger.json", "Elsa");
                swaggerOptions.DocExpansion(DocExpansion.None);
                swaggerOptions.OAuthUsePkce();
            });
            //}

        }

        app.UseSerilogRequestLogging();
        //app.Run();
    }
}
