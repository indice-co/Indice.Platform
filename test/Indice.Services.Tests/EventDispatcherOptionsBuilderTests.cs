using System.Security.Claims;
using Xunit;

namespace Indice.Services.Tests;

public class EventDispatcherOptionsBuilderTests
{
    [Fact]
    public void CanBuildEventDispatcherOptions() {
        var builder = new EventDispatcherRaiseOptionsBuilder();
        var options = builder.UsingPrincipal(new ClaimsPrincipal(
                                 new ClaimsIdentity(
                                     [new Claim("first_name", "Jack")])
                                 )
                             )
                             .Delay(TimeSpan.FromHours(1))
                             .WrapInEnvelope(false)
                             .WithQueueName("invoice-created")
                             .PrependEnvironmentInQueueName(false)
                             .Build();
        Assert.Equal("Jack", options.ClaimsPrincipal!.FindFirstValue("first_name"));
        Assert.Equal(TimeSpan.FromHours(1), options.VisibilityTimeout);
        Assert.False(options.Wrap);
        Assert.Equal("invoice-created", options.QueueName);
        Assert.False(options.PrependEnvironmentInQueueName);
    }

    [Fact]
    public void CanBuildEventDispatcherRaiseOptionsWithSessionId() {
        var builder = new EventDispatcherRaiseOptionsBuilder();
        var options = builder.UsingPrincipal(new ClaimsPrincipal(
                                 new ClaimsIdentity(
                                     [new Claim("first_name", "Jane")])
                                 )
                             )
                             .Delay(TimeSpan.FromMinutes(30))
                             .WrapInEnvelope(true)
                             .WithQueueName("order-created")
                             .PrependEnvironmentInQueueName(true)
                             .WithSessionId("session-123")
                             .Build();
        Assert.Equal("Jane", options.ClaimsPrincipal!.FindFirstValue("first_name"));
        Assert.Equal(TimeSpan.FromMinutes(30), options.VisibilityTimeout);
        Assert.True(options.Wrap);
        Assert.Equal("order-created", options.QueueName);
        Assert.True(options.PrependEnvironmentInQueueName);
        Assert.Equal("session-123", options.SessionId);
    }
}
