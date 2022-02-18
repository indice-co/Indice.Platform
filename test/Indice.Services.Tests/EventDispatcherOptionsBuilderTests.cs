using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace Indice.Services.Tests
{
    public class EventDispatcherOptionsBuilderTests
    {
        [Fact]
        public void CanBuildEventDispatcherOptions() {
            var builder = new EventDispatcherRaiseOptionsBuilder();
            var options = builder.UsingPrincipal(new ClaimsPrincipal(
                                     new ClaimsIdentity(
                                         new List<Claim> { new Claim("first_name", "Jack") })
                                     )
                                 )
                                 .Delay(TimeSpan.FromHours(1))
                                 .WrapInEnvelope(false)
                                 .WithQueueName("invoice-created")
                                 .PrependEnvironmentInQueueName(false)
                                 .Build();
            Assert.Equal("Jack", options.ClaimsPrincipal.FindFirstValue("first_name"));
            Assert.Equal(TimeSpan.FromHours(1), options.VisibilityTimeout);
            Assert.False(options.Wrap);
            Assert.Equal("invoice-created", options.QueueName);
            Assert.False(options.PrependEnvironmentInQueueName);
        }
    }
}
