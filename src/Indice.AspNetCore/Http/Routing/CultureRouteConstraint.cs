namespace Microsoft.AspNetCore.Routing.Constraints;


/// <summary>
/// Culture route constratint.
/// </summary>
public class CultureRouteConstraint : RegexRouteConstraint
{
    /// <summary>
    /// Constructor
    /// </summary>
    public CultureRouteConstraint()
        : base(@"^[a-zA-Z]{2}(\-[a-zA-Z]{2})?$") { }
}
