using Indice.Features.GeoIP;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class IPLocatorTests : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;
    public IPLocatorTests() {
        var services = new ServiceCollection();
        services.AddGeoIPResolver();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void GetLocationMetadata_Returns_Correct_Metadata() {
        var ipAddressLocator = _serviceProvider.GetRequiredService<IPAddressLocator>();
        var locationMetadata = ipAddressLocator.GetLocationMetadata(System.Net.IPAddress.Parse("8.8.8.8"));
        Assert.NotNull(locationMetadata);
        Assert.Equal("Google LLC", locationMetadata.AsOrganization);
    }

    public async Task DisposeAsync() {
        if (_serviceProvider != null) {
            await _serviceProvider.DisposeAsync();
        }   
    }

    public Task InitializeAsync() => Task.CompletedTask;
}
