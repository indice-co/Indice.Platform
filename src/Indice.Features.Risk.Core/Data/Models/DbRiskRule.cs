namespace Indice.Features.Risk.Core.Data.Models;

/// <summary>
/// Models the risk rule lookup name.
/// </summary>
public class DbRiskRule
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The lookup name registered in the system.
    /// </summary>
    public string Name { get; set; }
}
