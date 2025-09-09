using Indice.Features.GeoIP;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Enums;
using Indice.Features.Risk.Core.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.Risk.Core.Services;

/// <summary>Manages the rule executions of the risk engine.</summary>
public class RiskService
{
    /// <summary>The IP address locator service.</summary>
    private readonly IPAddressLocator _ipAddressLocator;
    /// <summary>The collection of rules registered in the risk engine.</summary>
    public IEnumerable<RiskRule> Rules { get; }
    /// <summary>Options used to configure the core risk engine.</summary>
    public RiskEngineOptions RiskEngineOptions { get; }

    /// <summary>Creates a new instance of <see cref="RiskService"/>.</summary>
    /// <param name="rules">Collection of rules registered in the engine.</param>
    /// <param name="riskEngineOptions">Options used to configure the core risk engine.</param>
    /// <param name="ipAddressLocator">The IP address locator service.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RiskService(
        IPAddressLocator ipAddressLocator,
        IEnumerable<RiskRule> rules,
        IOptions<RiskEngineOptions> riskEngineOptions
    ) {
        _ipAddressLocator = ipAddressLocator ?? throw new ArgumentNullException(nameof(ipAddressLocator));
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        RiskEngineOptions = riskEngineOptions.Value ?? throw new ArgumentNullException(nameof(riskEngineOptions));
    }

    /// <summary>Gets the risk score for a given event.</summary>
    /// <param name="model">The event occurred for which to calculate the risk score.</param>
    public async Task<AggregateRuleExecutionResult> GetRiskAsync(RiskModel model) {
        var results = new List<RuleExecutionResult>();
        var dbEvent = RiskMapper.ToRiskEvent(model, _ipAddressLocator);
        foreach (var rule in Rules.Where(x => x.Options.Enabled)) {
            var result = await rule.ExecuteAsync(dbEvent);
            result.RuleName = rule.Name;
            result.RiskLevel = RiskEngineOptions.RiskLevelRangeMapping.GetRiskLevel(result.RiskScore) ?? RiskLevel.None;
            results.Add(result);
        }
        return new AggregateRuleExecutionResult(dbEvent.Id, Rules.Count(), results, RiskEngineOptions);
    }
}
