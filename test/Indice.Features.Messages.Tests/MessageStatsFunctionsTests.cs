using Indice.Features.Messages.Core;
using Indice.Features.Messages.Worker.Azure;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Indice.Features.Messages.Tests;

/// <summary>Tests which Azure Functions are left out of the host, with the send statistics timer function added to the existing rules.</summary>
public class MessageStatsFunctionsTests
{
    private const string StatsVerify = "MessagingStatsVerify";
    private const string DatabaseCleanUp = "MessagingDatabaseCleanUp";
    private const string ServiceBusSendEmail = "servicebus-" + EventNames.SendEmail;

    [Theory]
    [InlineData(true, true, "0 30 0 * * *", false)]
    [InlineData(true, false, "0 30 0 * * *", true)]
    [InlineData(false, true, "0 30 0 * * *", true)]
    [InlineData(true, true, null, true)]
    [InlineData(true, true, " ", true)]
    public void StatsVerifyFunction_IsLeftOut_UnlessStatisticsAreOnAndScheduled(bool analyticsEnabled, bool statsEnabled, string? cronExpression, bool expectedExcluded) {
        var options = new AnalyticsOptions { Enabled = analyticsEnabled, Stats = new MessageStatsOptions { Enabled = statsEnabled } };
        var predicate = HostBuilderExtensions.ExcludeStatsVerifyUnlessConfigured(HostBuilderExtensions.ExcludeServiceBusTriggers, options);

        Assert.Equal(expectedExcluded, predicate(Function(StatsVerify), Configuration(cronExpression)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExistingExclusions_StillApply(bool statsEnabled) {
        var options = new AnalyticsOptions { Stats = new MessageStatsOptions { Enabled = statsEnabled } };
        var configuration = Configuration("0 30 0 * * *");

        var queuePredicate = HostBuilderExtensions.ExcludeStatsVerifyUnlessConfigured(HostBuilderExtensions.ExcludeServiceBusTriggers, options);
        Assert.True(queuePredicate(Function(ServiceBusSendEmail), configuration));
        Assert.False(queuePredicate(Function(EventNames.SendEmail), configuration));
        Assert.False(queuePredicate(Function(DatabaseCleanUp), configuration));

        var serviceBusPredicate = HostBuilderExtensions.ExcludeStatsVerifyUnlessConfigured(HostBuilderExtensions.ExcludeQueueTriggers, options);
        Assert.True(serviceBusPredicate(Function(EventNames.SendEmail), configuration));
        Assert.False(serviceBusPredicate(Function(ServiceBusSendEmail), configuration));
        Assert.False(serviceBusPredicate(Function(DatabaseCleanUp), configuration));
    }

    private static IFunctionMetadata Function(string name) => new DefaultFunctionMetadata { Name = name };

    private static IConfiguration Configuration(string? cronExpression) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> {
            ["MessageJobsOptions:StatsVerifyCronExpression"] = cronExpression
        })
        .Build();
}
