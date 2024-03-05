using System.Text.Json;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk rule lookup names.</summary>
public interface IRiskRuleStore
{
    /// <summary>
    /// Fetches the risk rule names registered in the system.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<string>> GetList();

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
    Task UpdateRiskRuleOptions(string ruleName, JsonElement jsonElement);
}
