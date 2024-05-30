namespace Indice.Features.Risk.Core.Models;

/// <summary>
/// Models a risk rule with its description and state.
/// </summary>
/// <param name="name">The name of the rule.</param>
public class RiskRuleDto(string name)
{
    /// <summary>
    /// The rule name.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The friendly name of the rule.
    /// </summary>
    public string? FriendlyName { get; set; }

    /// <summary>
    /// The rule description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the rule is enabled or not.
    /// </summary>
    public bool Enabled { get; set; }
}
