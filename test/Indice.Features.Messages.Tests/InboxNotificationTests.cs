using System.Text.Json.Nodes;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Rendering;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Indice.Features.Messages.Tests;

/// <summary>Covers the real time inbox notifications (<see cref="IInboxNotifier"/>).</summary>
public class InboxNotificationTests
{
    private const string RecipientId = "user-1";

    [Fact]
    public async Task SignalRNotifier_NotifyUser_BroadcastsToUserThroughConfiguredHubAndMethod() {
        var broadcastService = new Mock<ISignalRProxyBroadcastService>();
        SignalRBroadcastCommand? command = null;
        broadcastService
            .Setup(x => x.BroadcastToUserAsync("my-hub", RecipientId, It.IsAny<SignalRBroadcastCommand>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, SignalRBroadcastCommand, CancellationToken>((_, _, c, _) => command = c)
            .Returns(Task.CompletedTask);
        var notifier = new InboxNotifierSignalR(broadcastService.Object, Options.Create(new MessageInboxNotificationOptions { HubName = "my-hub", MethodName = "newMessage" }));
        var messageId = Guid.NewGuid();

        await notifier.NotifyUserAsync(RecipientId, new InboxMessageNotification { Id = messageId, Title = "Hello" });

        broadcastService.Verify(x => x.BroadcastToUserAsync("my-hub", RecipientId, It.IsAny<SignalRBroadcastCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(command);
        Assert.Equal("newMessage", command!.Method);
        var payload = Assert.IsType<JsonObject>(command.Message);
        Assert.Equal(messageId.ToString(), payload["id"]!.GetValue<string>());
        Assert.Equal("Hello", payload["title"]!.GetValue<string>());
    }

    [Fact]
    public async Task SignalRNotifier_NotifyAll_RespectsNotifyGlobalMessagesOption() {
        var broadcastService = new Mock<ISignalRProxyBroadcastService>();
        var enabled = new InboxNotifierSignalR(broadcastService.Object, Options.Create(new MessageInboxNotificationOptions()));
        var disabled = new InboxNotifierSignalR(broadcastService.Object, Options.Create(new MessageInboxNotificationOptions { NotifyGlobalMessages = false }));

        await enabled.NotifyAllAsync(new InboxMessageNotification { Id = Guid.NewGuid(), IsGlobal = true });
        await disabled.NotifyAllAsync(new InboxMessageNotification { Id = Guid.NewGuid(), IsGlobal = true });

        broadcastService.Verify(x => x.BroadcastToUsersAsync(
            MessageInboxNotificationOptions.DefaultHubName,
            It.Is<SignalRBroadcastCommand>(c => c.Method == MessageInboxNotificationOptions.DefaultMethodName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AddMessageInboxSignalRNotifications_ReplacesDefaultNotifier() {
        var services = new ServiceCollection();
        services.AddTransient<IInboxNotifier, InboxNotifierNoop>();

        services.AddMessageInboxSignalRNotifications(options => options.HubName = "inbox");

        var descriptor = Assert.Single(services, x => x.ServiceType == typeof(IInboxNotifier));
        Assert.Equal(typeof(InboxNotifierSignalR), descriptor.ImplementationType);
        Assert.Contains(services, x => x.ServiceType == typeof(ISignalRProxyBroadcastService));
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<MessageInboxNotificationOptions>>().Value;
        Assert.Equal("inbox", options.HubName);
    }

    [Fact]
    public async Task ResolveMessage_WithInboxChannel_NotifiesRecipientWithPersonalizedTitle() {
        var notifier = new Mock<IInboxNotifier>();
        var handler = CreateResolveMessageHandler(notifier.Object);
        var @event = CreateResolveMessageEvent(activePeriod: null);

        await handler.Process(@event);

        notifier.Verify(x => x.NotifyUserAsync(
            RecipientId,
            It.Is<InboxMessageNotification>(n => n.Id == @event.Campaign!.Id && n.Title == "Hello John" && !n.IsGlobal),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveMessage_NotYetActive_DoesNotNotify() {
        var notifier = new Mock<IInboxNotifier>();
        var handler = CreateResolveMessageHandler(notifier.Object);

        await handler.Process(CreateResolveMessageEvent(activePeriod: new Period { From = DateTimeOffset.UtcNow.AddDays(1) }));

        notifier.Verify(x => x.NotifyUserAsync(It.IsAny<string>(), It.IsAny<InboxMessageNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveMessage_NotifierFailure_DoesNotFailProcessing(bool canceled) {
        var notifier = new Mock<IInboxNotifier>();
        notifier.Setup(x => x.NotifyUserAsync(It.IsAny<string>(), It.IsAny<InboxMessageNotification>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceled ? new TaskCanceledException("SignalR timed out") : new InvalidOperationException("SignalR is down"));
        var messageService = new Mock<IMessageService>();
        messageService.Setup(x => x.Create(It.IsAny<CreateMessageRequest>())).ReturnsAsync(Guid.NewGuid());
        var dispatcherFactory = new Mock<IEventDispatcherFactory> { DefaultValue = DefaultValue.Mock };
        var handler = CreateResolveMessageHandler(notifier.Object, messageService, dispatcherFactory);
        var @event = CreateResolveMessageEvent(activePeriod: null);
        @event.Campaign!.MessageChannelKind |= MessageChannelKind.Email;
        @event.Campaign.Content[nameof(MessageChannelKind.Email)] = new MessageContent("Hello", "Body");

        await handler.Process(@event);

        messageService.Verify(x => x.Create(It.IsAny<CreateMessageRequest>()), Times.Once);
        var dispatcher = Mock.Get(dispatcherFactory.Object.Create(KeyedServiceNames.EventDispatcherServiceKey));
        Assert.Single(dispatcher.Invocations, x => x.Arguments[0] is SendEmailEvent);
    }

    [Fact]
    public async Task CampaignCreated_GlobalPublishedInboxCampaign_NotifiesAllUsers() {
        var notifier = new Mock<IInboxNotifier>();
        var handler = new CampaignCreatedHandler(Mock.Of<IEventDispatcherFactory>(), Mock.Of<IContactService>(), Mock.Of<ICampaignService>(), notifier.Object);
        var campaign = CreateCampaign(isGlobal: true, activePeriod: null);

        await handler.Process(campaign);

        notifier.Verify(x => x.NotifyAllAsync(
            It.Is<InboxMessageNotification>(n => n.Id == campaign.Id && n.IsGlobal && n.Title == "Hello {{contact.firstName}}"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CampaignCreated_GlobalUnpublishedOrExpiredCampaign_DoesNotNotify() {
        var notifier = new Mock<IInboxNotifier>();
        var handler = new CampaignCreatedHandler(Mock.Of<IEventDispatcherFactory>(), Mock.Of<IContactService>(), Mock.Of<ICampaignService>(), notifier.Object);
        var unpublished = CreateCampaign(isGlobal: true, activePeriod: null);
        unpublished.Published = false;

        await handler.Process(unpublished);
        await handler.Process(CreateCampaign(isGlobal: true, activePeriod: new Period { To = DateTimeOffset.UtcNow.AddDays(-1) }));

        notifier.Verify(x => x.NotifyAllAsync(It.IsAny<InboxMessageNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CampaignCreated_NotifierFailure_DoesNotFailProcessing(bool canceled) {
        var notifier = new Mock<IInboxNotifier>();
        notifier.Setup(x => x.NotifyAllAsync(It.IsAny<InboxMessageNotification>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceled ? new TaskCanceledException("SignalR timed out") : new InvalidOperationException("SignalR is down"));
        var dispatcherFactory = new Mock<IEventDispatcherFactory> { DefaultValue = DefaultValue.Mock };
        var handler = new CampaignCreatedHandler(dispatcherFactory.Object, Mock.Of<IContactService>(), Mock.Of<ICampaignService>(), notifier.Object);
        var campaign = CreateCampaign(isGlobal: true, activePeriod: null);
        campaign.MessageChannelKind |= MessageChannelKind.PushNotification;
        campaign.Content[nameof(MessageChannelKind.PushNotification)] = new MessageContent("Hello", "Body");

        await handler.Process(campaign);

        notifier.Verify(x => x.NotifyAllAsync(It.IsAny<InboxMessageNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        var dispatcher = Mock.Get(dispatcherFactory.Object.Create(KeyedServiceNames.EventDispatcherServiceKey));
        Assert.Single(dispatcher.Invocations, x => x.Arguments[0] is SendPushNotificationEvent);
    }

    private static ResolveMessageHandler CreateResolveMessageHandler(IInboxNotifier notifier, Mock<IMessageService>? messageService = null, Mock<IEventDispatcherFactory>? eventDispatcherFactory = null) {
        var contactService = new Mock<IContactService>();
        contactService.Setup(x => x.GetByRecipientId(RecipientId)).ReturnsAsync(CreateContact());
        if (messageService is null) {
            messageService = new Mock<IMessageService>();
            messageService.Setup(x => x.Create(It.IsAny<CreateMessageRequest>())).ReturnsAsync(Guid.NewGuid());
        }
        eventDispatcherFactory ??= new Mock<IEventDispatcherFactory> { DefaultValue = DefaultValue.Mock };
        var partialTemplateResolverFactory = new Mock<IPartialTemplateResolverFactory> { DefaultValue = DefaultValue.Mock };
        return new ResolveMessageHandler(
            eventDispatcherFactory.Object,
            Mock.Of<IContactResolver>(),
            contactService.Object,
            messageService.Object,
            NullLogger<ResolveMessageHandler>.Instance,
            Options.Create(new MessageWorkerOptions()),
            new MessageEventQueue(Options.Create(new AnalyticsOptions())),
            partialTemplateResolverFactory.Object,
            notifier);
    }

    private static ResolveMessageEvent CreateResolveMessageEvent(Period? activePeriod) => new() {
        Campaign = CreateCampaign(isGlobal: false, activePeriod),
        Contact = CreateContact()
    };

    private static CampaignCreatedEvent CreateCampaign(bool isGlobal, Period? activePeriod) => new() {
        Id = Guid.NewGuid(),
        Title = "Campaign",
        Published = true,
        IsGlobal = isGlobal,
        IgnoreUserPreferences = true,
        ActivePeriod = activePeriod,
        MessageChannelKind = MessageChannelKind.Inbox,
        Content = new MessageContentDictionary {
            [nameof(MessageChannelKind.Inbox)] = new MessageContent("Hello {{contact.firstName}}", "<p>Body</p>")
        }
    };

    private static Contact CreateContact() => new() {
        Id = Guid.NewGuid(),
        RecipientId = RecipientId,
        FirstName = "John",
        Email = "john@example.com",
        LastResolutionDate = DateTimeOffset.UtcNow
    };
}
