using Indice.Events;
using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Models;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.ActivityLog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Indice.Features.Identity.Tests;

public class UserRolesChangedActivityLogTests
{
    [Theory]
    [InlineData(UserRoleChangeType.Added, "Developer", false, "Role 'Developer' added")]
    [InlineData(UserRoleChangeType.Removed, "Administrator", false, "Role 'Administrator' removed")]
    [InlineData(UserRoleChangeType.Added, UserRolesChangedEvent.GlobalAdminRole, true, "Global administrator access added")]
    [InlineData(UserRoleChangeType.Removed, UserRolesChangedEvent.GlobalAdminRole, true, "Global administrator access removed")]
    [InlineData(UserRoleChangeType.Added, UserRolesChangedEvent.GlobalAdminRole, false, "Role 'GlobalAdmin' added")]
    public async Task PublishedRoleChanges_ProduceActivityLog_WithRoleDetails(UserRoleChangeType changeType, string role, bool isGlobalAdmin, string description) {
        var entries = new List<ActivityLogEntry>();
        var publisher = new Mock<IActivityLogPublisher>();
        publisher.Setup(x => x.PublishAsync(It.IsAny<ActivityLogEntry>()))
                 .Callback<ActivityLogEntry>(entries.Add)
                 .Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(publisher.Object);
        services.AddDefaultPlatformEventService();
        var options = new ActivityLogIdentityOptions(services, new ConfigurationBuilder().Build());
        options.ListenToPlatformEvents<IdentityEventsActivityLogConverter>();
        await using var provider = services.BuildServiceProvider();
        var user = UserEventContext.InitializeFromUser(new User("affected.user") { Id = "user-123" });
        var changed = new UserRolesChangedEvent(user, changeType, role, isGlobalAdmin);

        await provider.GetRequiredService<IPlatformEventService>().Publish(changed);

        var entry = Assert.Single(entries);
        Assert.Equal(nameof(UserRolesChangedEvent), entry.EventType);
        Assert.Equal(ActivityLogCategories.User, entry.Category);
        Assert.Equal("user-123", entry.ResourceId);
        Assert.Equal("User", entry.ResourceType);
        Assert.Equal(description, entry.Description);
        Assert.True(entry.Succeeded);
        Assert.Null(entry.SubjectId);
        Assert.Null(entry.SubjectName);
    }
}
