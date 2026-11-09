using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Services.Tests;
public class KeyedServiceTests
{
    [Fact]
    public void KeyedServices_Can_have_Multiple_Configurations_Same_Implementation() {

        var inMemorySettings = new Dictionary<string, string?> {};
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var factory = new AzureClientFactory(configuration);
        var collection = new ServiceCollection()
            .AddSingleton(configuration)
            .AddOptions()
            .AddTransient<IFileService>(sp => new FileServiceAzureStorage(factory, FileServiceAzureStorage.CONNECTION_STRING_NAME, "serviceDefault"))
            .AddKeyedTransient<IFileService, FileServiceInMemory>("serviceA")
            .AddKeyedTransient<IFileService, FileServiceAzureStorage>("serviceB", (sp, key) => new FileServiceAzureStorage(factory, FileServiceAzureStorage.CONNECTION_STRING_NAME, key!.ToString()))
            .AddKeyedTransient<IFileService, FileServiceAzureStorage>("serviceC", (sp, key) => new FileServiceAzureStorage(factory, FileServiceAzureStorage.CONNECTION_STRING_NAME, key!.ToString()));
        var serviceProvider = collection.BuildServiceProvider();        

        var serviceDefault = serviceProvider.GetRequiredService<IFileService>();
        var serviceB = serviceProvider.GetKeyedService<IFileService>("serviceB");
        var serviceC = serviceProvider.GetKeyedService<IFileService>("serviceC");

        Assert.IsType<FileServiceAzureStorage>(serviceDefault);
        Assert.IsType<FileServiceAzureStorage>(serviceB);
        Assert.IsType<FileServiceAzureStorage>(serviceC);

        var containerName = typeof(FileServiceAzureStorage)
            .GetField("_containerName", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var connectionB = containerName.GetValue(serviceB)!;
        var connectionC = containerName.GetValue(serviceC)!;
        var connectionDefault = containerName.GetValue(serviceDefault)!;

        Assert.Equal("serviceB", connectionB.ToString(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("serviceC", connectionC.ToString(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("serviceDefault", connectionDefault.ToString(), StringComparer.OrdinalIgnoreCase);
    }
}
