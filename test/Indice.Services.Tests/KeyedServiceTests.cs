using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Services.Tests;
public class KeyedServiceTests
{

    [Fact]
    public void KeyedServices_Can_have_Multiple_Configurations_Same_Implementation() {

        var inMemorySettings = new Dictionary<string, string> {
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var collection = new ServiceCollection()
            .AddSingleton(configuration)
            .AddOptions()
            .AddKeyedScoped<IFileService, FileServiceInMemory>("serviceA")
            .AddKeyedScoped<IFileService, FileServiceAzureStorage>("serviceB", (sp, key) => new FileServiceAzureStorage(key.ToString(), key.ToString()))
            .AddKeyedScoped<IFileService, FileServiceAzureStorage>("serviceC", (sp, key) => new FileServiceAzureStorage(key.ToString(), key.ToString()));
        var serviceProvider = collection.BuildServiceProvider();

        var serviceB = serviceProvider.GetKeyedService<IFileService>("serviceB");
        var serviceC = serviceProvider.GetKeyedService<IFileService>("serviceC");

        Assert.IsType<FileServiceAzureStorage>(serviceB);
        Assert.IsType<FileServiceAzureStorage>(serviceC);
        var connectionString = typeof(Indice.Services.FileServiceAzureStorage).GetField("_connectionString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.Equal("serviceB", connectionString.GetValue(serviceB));
        Assert.Equal("serviceC", connectionString.GetValue(serviceC));
    }
}
