using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Enums;
using Indice.Features.Risk.Core.Extensions;
using Indice.Features.Risk.Core.Models;
using Indice.Features.Risk.Core.Services;
using Indice.Features.Risk.Core.Types;
using Indice.Features.Risk.Core.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Tests;

public class TestRule : RiskRule
{
    public TestRule() : base("TransactionOver1000") {
        Options = new RuleOptions {
            Enabled = true
        };
    }

    public override ValueTask<RuleExecutionResult> ExecuteAsync(RiskEvent @event) {
        return ValueTask.FromResult(
            @event.Name == "Transaction" && @event.Amount >= 1000
                ? RuleExecutionResult.HighRisk()
                : RuleExecutionResult.LowRisk()
        );
    }
}

public class RiskCalculationTests
{
    public RiskCalculationTests() {
        // Initialize the default collection of service descriptors.
        var services = new ServiceCollection();
        // Setup risk engine.
        services.AddRiskEngine(options => {
            options.RiskLevelRangeMapping = new RiskLevelRangeDictionary(new Dictionary<RiskLevel, IntegerRange> {
                [RiskLevel.None] = new IntegerRange(0, 0),
                [RiskLevel.Low] = new IntegerRange(1, 1000),
                [RiskLevel.Medium] = new IntegerRange(1001, 2000),
                [RiskLevel.High] = new IntegerRange(2001, 3000)
            });
        })
        .AddRule<TestRule, RuleOptions, RuleOptionsValidator<RuleOptions>>("TransactionOver1000")
        .AddEntityFrameworkCoreStore(builder => {
            builder.UseInMemoryDatabase("RiskDb");
        });
        // Initialize test class properties.
        ServiceProvider = services.BuildServiceProvider();
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();
    }

    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }

    [Fact]
    public async Task High_Risk_On_Transaction_Over_1000() {
        var riskManager = ServiceProvider.GetRequiredService<RiskService>();
        var result = await riskManager.GetRiskAsync(new RiskEvent {
            Amount = 1001,
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = "127.0.0.1",
            Name = "Transaction",
            SubjectId = "4075C988-ECDB-434D-8164-970F7DF39DC3"
        });
        Assert.Single(result.Results);
        Assert.Equal(1, result.NumberOfRulesExecuted);
        Assert.Equal(3000, result.Results.First().RiskScore);
        Assert.Equal(RiskLevel.High, result.Results.First().RiskLevel);
    }

    [Fact]
    public async Task Low_Risk_On_Transaction_Under_1000() {
        var riskManager = ServiceProvider.GetRequiredService<RiskService>();
        var result = await riskManager.GetRiskAsync(new RiskEvent {
            Amount = 999,
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = "127.0.0.1",
            Name = "Transaction",
            SubjectId = "4075C988-ECDB-434D-8164-970F7DF39DC3"
        });
        Assert.Single(result.Results);
        Assert.Equal(1, result.NumberOfRulesExecuted);
        Assert.Equal(1000, result.Results.First().RiskScore);
        Assert.Equal(RiskLevel.Low, result.Results.First().RiskLevel);
    }

    [Fact]
    public async Task Can_Create_Risk_Events() {
        const string SUBJECT_ID = "4075C988-ECDB-434D-8164-970F7DF39DC3";
        var riskManager = ServiceProvider.GetRequiredService<RiskStoreService>();
        await riskManager.CreateRiskEventAsync(new RiskEvent {
            Amount = 1001,
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = "127.0.0.1",
            Name = "Transaction",
            SubjectId = SUBJECT_ID
        });
        await riskManager.CreateRiskEventAsync(new RiskEvent {
            Amount = null,
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = "127.0.0.1",
            Name = "ProfileUpdate",
            SubjectId = SUBJECT_ID
        });
        var events = await riskManager.GetRiskEventsAsync(SUBJECT_ID, names: new string[] { "Transaction", "ProfileUpdate" });
        Assert.Equal(2, events.Count());
    }
}
