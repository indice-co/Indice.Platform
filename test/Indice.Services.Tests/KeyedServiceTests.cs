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
            .AddTransient<IFileService>(sp => new FileServiceAzureStorage("serviceDefault", "serviceDefault"))
            .AddKeyedTransient<IFileService, FileServiceInMemory>("serviceA")
            .AddKeyedTransient<IFileService, FileServiceAzureStorage>("serviceB", (sp, key) => new FileServiceAzureStorage(key.ToString(), key.ToString()))
            .AddKeyedTransient<IFileService, FileServiceAzureStorage>("serviceC", (sp, key) => new FileServiceAzureStorage(key.ToString(), key.ToString()));
        var serviceProvider = collection.BuildServiceProvider();

        var serviceDefault = serviceProvider.GetRequiredService<IFileService>();
        var serviceB = serviceProvider.GetKeyedService<IFileService>("serviceB");
        var serviceC = serviceProvider.GetKeyedService<IFileService>("serviceC");

        Assert.IsType<FileServiceAzureStorage>(serviceB);
        Assert.IsType<FileServiceAzureStorage>(serviceC);
        var connectionString = typeof(Indice.Services.FileServiceAzureStorage).GetField("_connectionString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
#if NET8_0_OR_GREATER
        Assert.Equal("serviceB", connectionString.GetValue(serviceB));
        Assert.Equal("serviceC", connectionString.GetValue(serviceC));
#else
        Assert.Equal("serviceC", connectionString.GetValue(serviceB)); // <-- Should be "serviceB"!!!!!. Although is expected behavior not to work some day we should make it be like dotnet 8 or abandon previous versions.
        Assert.Equal("serviceC", connectionString.GetValue(serviceC));
#endif
        Assert.Equal("serviceDefault", connectionString.GetValue(serviceDefault));
    }
}
