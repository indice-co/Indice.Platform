using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Services;
using Indice.Features.Risk.Core.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Risk.Tests;

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
                [RiskLevel.High] = new IntegerRange(2000, 3000)
            });
        })
        .AddRule("TransactionOver1000", riskEvent =>
            ValueTask.FromResult(
                riskEvent.Type == "Transaction" && riskEvent.Amount > 1000
                    ? RuleExecutionResult.HighRisk()
                    : RuleExecutionResult.LowRisk()
            )
        )
        .AddEntityFrameworkCoreStore(builder => {
            builder.UseSqlite("Data Source=.\\risk.db"); // Use 'DataSource=:memory:' for in-memory data source.
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
    public async void High_Risk_On_Transaction_Over_1000() {
        var riskManager = ServiceProvider.GetRequiredService<RiskManager>();
        var result = await riskManager.GetRiskAsync(new RiskEvent {
            Amount = 1001,
            CreatedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            Name = "domestic_transaction_e3f9f3bf-7ab7-414f-9307-0c815922ef0c",
            SubjectId = "4075C988-ECDB-434D-8164-970F7DF39DC3",
            Type = "Transaction"
        });
        Assert.Single(result.Results);
        Assert.Equal(1, result.NumberOfRulesExecuted);
        Assert.Equal(RiskLevel.High, result.Results.First().RiskLevel);
    }
}
