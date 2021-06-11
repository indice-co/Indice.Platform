using System.Threading.Tasks;
using CodeFlowMvc.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace CodeFlowMvc
{
    public class Startup
    {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddControllersWithViews();
            services.AddAuthentication(options => {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = IndiceDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOpenIdConnect(authenticationScheme: IndiceDefaults.AuthenticationScheme, options => {
                var indiceAuthSection = Configuration.GetSection("Auth:Indice");
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = indiceAuthSection.GetValue<string>("Authority");
                options.ClientId = indiceAuthSection.GetValue<string>("ClientId");
                options.ClientSecret = indiceAuthSection.GetValue<string>("ClientSecret");
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.CallbackPath = "/signin-indice";
                var scopes = indiceAuthSection.GetSection("Scopes").Get<string[]>();
                foreach (var scope in scopes) {
                    options.Scope.Add(scope);
                }
                options.Events = new OpenIdConnectEvents {
                    OnAuthorizationCodeReceived = context => {
                        return Task.CompletedTask;
                    },
                    OnUserInformationReceived = context => {
                        return Task.CompletedTask;
                    }
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
