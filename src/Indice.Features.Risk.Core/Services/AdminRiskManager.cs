using System.Text.Json;
using Indice.Features.Risk.Core.Abstractions;

namespace Indice.Features.Risk.Core.Services;

/// <summary>Manages the risk rule configurations registered in the risk engine.</summary>
public class AdminRiskManager
{
    /// <summary>The collection of rules registered in the risk engine.</summary>
    private readonly IEnumerable<RiskRule> _rules;

    /// <summary>The risk rules configuration store.</summary>
    private readonly IRiskRuleOptionsStore _riskRuleOptionsStore;

    /// <summary>
    /// Creates a new instance of <see cref="AdminRiskManager"/>.
    /// </summary>
    /// <param name="rules">Collection of rules registered in the engine.<</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AdminRiskManager(
        IEnumerable<RiskRule> rules,
        IRiskRuleOptionsStore riskRuleOptionsStore) {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _riskRuleOptionsStore = riskRuleOptionsStore ?? throw new ArgumentNullException(nameof(riskRuleOptionsStore));
    }

    /// <summary>
    /// Gets the list of risk rules registered in the system.
    /// </summary>
    /// <returns></returns>
    public List<string> GetRiskRules() {
        return _rules.Select(x => x.Name).ToList();
    }

    /// <summary>
    /// Gets the associated rule options, given a rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>> GetRiskRuleOptionsAsync(string ruleName) {
        return await _riskRuleOptionsStore.GetRiskRuleOptions(ruleName);
    }

    /// <summary>
    /// Updates the rule options, given a rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <param name="jsonData"></param>
    /// <returns></returns>
    public async Task UpdateRiskRuleOptionsAsync(string ruleName, JsonElement jsonData) {
        await _riskRuleOptionsStore.UpdateRiskRuleOptions(ruleName, jsonData);
    }
}
