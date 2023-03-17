using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Indice.AspNetCore.Tests;

public class TestStartup : IStartup
{
    public IServiceProvider ConfigureServices(IServiceCollection services) {
        services.AddMvc();
        services.AddHtmlRenderingEngineRazorMvc();
        services.AddSingleton(serviceProvider => new Mock<IHttpContextAccessor>().Object);
        return services.BuildServiceProvider();
    }

    public void Configure(IApplicationBuilder app) { }
}
