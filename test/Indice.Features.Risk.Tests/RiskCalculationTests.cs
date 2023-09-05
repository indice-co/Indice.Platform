using Indice.Features.Risk.Core;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Services;
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
        services.AddRiskEngine()
                .WithEntityFrameworkCoreStore(options => options.UseSqlite("Data Source=.\\risk.db")) // Use 'DataSource=:memory:' for in-memory data source.
                .AddRule("TransactionOver1000", riskEvent =>
                    ValueTask.FromResult(
                        riskEvent.Type == "Transaction" && riskEvent.Amount > 1000
                            ? RuleExecutionResult.HighRisk()
                            : RuleExecutionResult.LowRisk()
                    )
                );
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
        var result = await riskManager.GetRiskAsync(new DbRiskEvent {
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
