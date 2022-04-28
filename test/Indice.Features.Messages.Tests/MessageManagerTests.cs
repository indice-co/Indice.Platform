﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Manager.Commands;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Services;
using Indice.Types;
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
                .AddTransient<NotificationsManager>()
                .AddTransient<ICampaignService, CampaignService>()
                .AddTransient<IContactService, ContactService>()
                .AddTransient<IMessageTypeService, MessageTypeService>()
                .AddTransient<IDistributionListService, DistributionListService>()
                .AddTransient<ITemplateService, TemplateService>()
                .AddTransient<CreateCampaignRequestValidator>()
                .AddTransient<UpsertMessageTypeRequestValidator>()
                .AddTransient<Func<string, IEventDispatcher>>(serviceProvider => key => new EventDispatcherNoop())
                .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
                //.AddKeyedService<IFileService, FileServiceInMemory, string>(KeyedServiceNames.FileServiceKey, ServiceLifetime.Singleton)
                //.AddFiles(x => x.AddFilesInMemory())
                .AddOptions()
                .Configure<MessageManagementOptions>(configuration);
            ServiceProvider = services.BuildServiceProvider();
        }

        public ServiceProvider ServiceProvider { get; }

        [Fact]
        public void CanInstantiateMessageManager() {
            var manager = ServiceProvider.GetRequiredService<NotificationsManager>();
            Assert.IsType<NotificationsManager>(manager);
        }

        [Fact]
        public async Task CanCreateCampaignWithMinimumFields() {
            var manager = ServiceProvider.GetRequiredService<NotificationsManager>();
            var campaign = new CreateCampaignCommand {
                IsGlobal = true,
                MessageChannelKind = MessageChannelKind.Inbox,
                Published = false,
                Title = "Welcome"
            };
            var result = await manager.CreateCampaign(campaign);
            Assert.True(result.Succeeded);
            Assert.NotEqual(default, result.CampaignId);
        }

        [Fact]
        public async Task CanCreateCampaignWithAllFields() {
            var manager = ServiceProvider.GetRequiredService<NotificationsManager>();
            var campaign = new CreateCampaignCommand {
                ActionLink = new Hyperlink {
                    Href = "https://www.google.com",
                    Text = "Google"
                },
                ActivePeriod = new Period { From = DateTimeOffset.UtcNow },
                Content = new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Inbox] = new MessageContent { Title = "Welcome", Body = "Dear {{contact.fullName}}, thank you for registering." }
                },
                Data = new {
                    Code = "123abc"
                },
                IsGlobal = true,
                MessageChannelKind = MessageChannelKind.Inbox,
                Published = false,
                RecipientIds = new List<string> { "ab9769f1-d532-4b7d-9922-3da003157ebd" },
                Title = "Welcome"
            };
            var result = await manager.CreateCampaign(campaign);
            Assert.True(result.Succeeded);
            Assert.NotEqual(default, result.CampaignId);
        }

        [Fact]
        public async Task CanCreateCampaignUsingDifferentContent() {
            var manager = ServiceProvider.GetRequiredService<NotificationsManager>();
            var result = await manager.SendMessageToRecipient(
                recipientId: Guid.NewGuid().ToString(),
                title: "Welcome",
                channels: MessageChannelKind.Inbox | MessageChannelKind.PushNotification,
                templates: new Dictionary<MessageChannelKind, MessageContent> {
                    [MessageChannelKind.Inbox] = new MessageContent("Welcome", "Hello {{contact.Salutation}} {{contact.FullName}} and welcome to our company."),
                    [MessageChannelKind.PushNotification] = new MessageContent("Welcome", "Hello {{contact.Salutation}} {{contact.FullName}} and welcome to our company.")
                }
            );
            Assert.True(result.Succeeded);
            Assert.NotEqual(default, result.CampaignId);
        }

        [Fact]
        public async Task CanCreateCampaignUsingSameContent() {
            var manager = ServiceProvider.GetRequiredService<NotificationsManager>();
            var result = await manager.SendMessageToRecipient(
                recipientId: Guid.NewGuid().ToString(),
                title: "Welcome",
                channels: MessageChannelKind.Inbox | MessageChannelKind.PushNotification,
                template: new MessageContent("Welcome", "Hello {{contact.Salutation}} {{contact.FullName}} and welcome to our company.")
            );
            Assert.True(result.Succeeded);
            Assert.NotEqual(default, result.CampaignId);
        }

        public ValueTask DisposeAsync() {
            GC.SuppressFinalize(this);
            return ServiceProvider.DisposeAsync();
        }


        public class Container { 
            public dynamic Data { get; set; } 
        }

        [Fact]
        public void ToExpandoBug() {
            var data = new {
                uid = Guid.NewGuid(),
                nubmer = new int?(),
                currency = new decimal?(20.8M),
                currency2 = new decimal?(),
                title = "Σας ευχαριστούμε!",
                subTitle = "Δέσμευση φορτιστή",
                invoicePdfLink = "reservationTransaction.InvoiceAttachmentLink",
                invoiceLink = "reservationTransaction.InvoiceLink",
                charger = "reservation.ChargePointName",
                chargerImage = "reservation.ChargePointImage",
                connector = $"Θύρα φόρτισης 1",
                when = "dd/M/yyyy HH:mm",
                duration = "",
                expiration = "dd/M/yyyy HH:mm",
                netCost = "reservation.TotalPriceNet",
                vatCost = "reservation.TotalPriceVat",
                totalCost = "reservation.TotalPrice",
                publicNotes = "reservation.GetInvoicePublicNotes()",
                paymentInfo = "walletTransaction.GetInvoicePaymentNotes(paymentMethod)",
                locationName = "reservation.LocationName",
                locationAddress = "reservation.LocationAddress",
                locationMap = "https://www.evpulse.eu/image-assets/location-map/{reservation.LocationId}",
                locationMapLink = "",
                footer = "Σας ευχαριστούμε που επιλέξατε την υπηρεσία EVPulse",
                list = new[] { 
                    new Contact { FullName ="Kvnst"  }
                }
            };
            var container = new Container() { Data = data };
            var data2 = JsonSerializer.Deserialize<Container>( JsonSerializer.Serialize(container));

            var result = ExpandoTest(data2.Data);
            Assert.True(true);
        }

        private ExpandoObject ExpandoTest(dynamic data) {
            return Mapper.ToExpandoObject(data);
        }
    }
}