using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.GeoIP;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class IPLocatorTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider;
    public IPLocatorTests() {
        var services = new ServiceCollection();
        services.AddGeoIPResolver();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetLocationMetadata_Returns_Correct_Metadata() {
        var ipAddressLocator = _serviceProvider.GetRequiredService<IPAddressLocator>();
        var locationMetadata = ipAddressLocator.GetLocationMetadata(System.Net.IPAddress.Parse("8.8.8.8"));
        Assert.NotNull(locationMetadata);
        Assert.Equal("Google LLC", locationMetadata.ASOrganization);
    }

    public async Task DisposeAsync() {
        if (_serviceProvider != null) {
            await _serviceProvider.DisposeAsync();
        }   
    }

    public Task InitializeAsync() => Task.CompletedTask;
}
