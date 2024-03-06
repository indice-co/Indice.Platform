using System.Text.Json;
using Indice.Features.Risk.Core.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk rule configuration.</summary>
public interface IRiskRuleOptionsStore
{
    /// <summary>
    /// Fetches the risk options for a given rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <returns></returns>
    Task<Dictionary<string, string>> GetRiskRuleOptions(string ruleName);

    /// <summary>
    /// Updates the risk options for a given rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <returns></returns>
    Task UpdateRiskRuleOptions(string ruleName, RuleOptions jsonElement);
}
