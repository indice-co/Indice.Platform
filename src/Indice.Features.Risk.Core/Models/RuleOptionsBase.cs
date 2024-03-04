namespace Indice.Features.Risk.Core.Models;

/// <summary>
/// Contains the basic options for each rule registered in the Risk Engine.
/// </summary>
public class RuleOptionsBase
{
    /// <summary>
    /// Name of the rule.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// A description of the rule.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Whether the rule is enabled or not.
    /// </summary>
    public bool Enabled { get; set; }
}
