namespace Indice.Features.Identity.UI;

/// <summary>The ui framework used by the razor pages</summary>
/// <remarks>
/// Each framework (except the <see cref="Bootstrap4"/> alias) ships its pages under <c>Pages/{Name}/</c> and its styles under
/// <c>wwwroot/css/{name}/</c>. To add a framework, add a value here and the matching page tree; the host selects it with the
/// <c>IdentityUIFrameworkVersion</c> MSBuild property.
/// </remarks>
internal enum UIFramework
{
    // The default framework for a given release must be 0.
    // So this needs to be updated in the future if we include more frameworks.
    /// <summary>Bootstrap 5</summary>
    Bootstrap5 = 0
}
