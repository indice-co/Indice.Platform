using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Models;
using Indice.Types;

namespace Indice.Features.Risk.Core.Services;

/// <summary>Manages the risk rule configurations registered in the risk engine.</summary>
public class AdminRuleManager
{
    /// <summary>The collection of rules registered in the risk engine.</summary>
    private readonly IEnumerable<RiskRule> _rules;

    /// <summary>The risk rules configuration store.</summary>
    private readonly IRuleOptionsStore _ruleOptionsStore;

    /// <summary>
    /// Creates a new instance of <see cref="AdminRuleManager"/>.
    /// </summary>
    /// <param name="rules">Collection of rules registered in the engine.</param>
    /// <param name="riskRuleOptionsStore">Rule store</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AdminRuleManager(
        IEnumerable<RiskRule> rules,
        IRuleOptionsStore riskRuleOptionsStore) {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _ruleOptionsStore = riskRuleOptionsStore ?? throw new ArgumentNullException(nameof(riskRuleOptionsStore));
    }

    /// <summary>
    /// Gets the list of risk rules registered in the system.
    /// </summary>
    /// <returns></returns>
    public ResultSet<RiskRuleDto> GetRiskRules() {
        var result = _rules
            .Select((x, index) => new RiskRuleDto {
                Name = x.Name,
                Description = x.Options.Description,
                Enabled = x.Options.Enabled,
                FriendlyName = x.Options.FriendlyName
            })
            .ToResultSet();
        return result;
    }

    /// <summary>
    /// Gets the associated rule options, given a rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>> GetRiskRuleOptionsAsync(string ruleName) {
        return await _ruleOptionsStore.GetRuleOptions(ruleName);
    }

    /// <summary>
    /// Updates the rule options, given a rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <param name="ruleOptions"></param>
    /// <returns></returns>
    public async Task UpdateRiskRuleOptionsAsync(string ruleName, RuleOptions ruleOptions) {
        await _ruleOptionsStore.UpdateRuleOptions(ruleName, ruleOptions);
    }
}
