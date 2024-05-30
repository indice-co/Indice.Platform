namespace Indice.Features.Risk.Core.Models;

/// <summary>
/// Contains the basic options for each rule registered in the Risk Engine.
/// </summary>
public class RuleOptions
{
    /// <summary>
    /// Friendly name of the rule.
    /// </summary>
    public string? FriendlyName { get; set; }

    /// <summary>
    /// The description of the rule.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the rule is enabled or not.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Eligible events for executing the associated rule, if any.
    /// </summary>
    public List<string> EligibleEvents { get; set; } = [];
}
