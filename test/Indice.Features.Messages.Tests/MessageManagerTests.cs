using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;

namespace Indice.Features.Messages.Tests
{
    public class MessageManagerTests : IAsyncDisposable
    {
        public MessageManagerTests() {
            var inMemorySettings = new Dictionary<string, string> {
                ["ConnectionStrings:MessagesDb"] = "Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test;Trusted_Connection=True;MultipleActiveResultSets=true"
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var services = new ServiceCollection()
                .AddLogging()
                .AddTransient<IHostEnvironment>(serviceProvider => new HostingEnvironment {
                    ApplicationName = typeof(MessageManagerTests).Assembly.GetName().Name,
                    EnvironmentName = Environments.Development,
                    ContentRootPath = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\"),
                    ContentRootFileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\"))
                })
                .AddDbContext<CampaignsDbContext>(builder => builder.UseInMemoryDatabase(databaseName: "MessagesDb"), ServiceLifetime.Singleton)
                .AddSingleton(configuration)
                .AddTransient<MessageManager>()
                .AddTransient<ICampaignService, CampaignService>()
                .AddTransient<IMessageTypeService, MessageTypeService>()
                .AddTransient<IDistributionListService, DistributionListService>()
                .AddTransient<ITemplateService, TemplateService>()
                .AddTransient<CreateCampaignRequestValidator>()
                .AddTransient<UpsertMessageTypeRequestValidator>()
                .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
                .AddKeyedService<IFileService, FileServiceInMemory, string>(KeyedServiceNames.FileServiceKey, ServiceLifetime.Singleton)
                .AddFiles(x => x.AddFilesInMemory())
                .AddOptions()
                .Configure<MessageManagementOptions>(configuration);
            ServiceProvider = services.BuildServiceProvider();
        }

        public ServiceProvider ServiceProvider { get; }

        [Fact]
        public void CanInstantiateMessageManager() {
            var manager = ServiceProvider.GetRequiredService<MessageManager>();
            Assert.IsType<MessageManager>(manager);
        }

        public ValueTask DisposeAsync() {
            return ServiceProvider.DisposeAsync();
        }
    }
}