﻿using Indice.Features.Risk.Core.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Store for risk rule configuration.</summary>
public interface IRuleOptionsStore
{
    /// <summary>
    /// Fetches the risk options for a given rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <returns></returns>
    Task<Dictionary<string, string?>> GetRuleOptions(string ruleName);

    /// <summary>
    /// Updates the risk options for a given rule name.
    /// </summary>
    /// <param name="ruleName"></param>
    /// <param name="ruleOptions"></param>
    /// <returns></returns>
    Task UpdateRuleOptions(string ruleName, RuleOptions ruleOptions);
}
