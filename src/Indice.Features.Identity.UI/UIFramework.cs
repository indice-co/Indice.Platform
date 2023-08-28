namespace Indice.Features.Identity.UI;

/// <summary>The ui framework used by the razor pages</summary>
internal enum UIFramework
{
    // The default framework for a given release must be 0.
    // So this needs to be updated in the future if we include more frameworks.
    /// <summary>Bootstrap 4</summary>
    Bootstrap4 = 0,
    /// <summary>Bootstrap 5</summary>
    Bootstrap5 = 1,
    /// <summary>Tailwind 3</summary>
    Tailwind3 = 2
}
