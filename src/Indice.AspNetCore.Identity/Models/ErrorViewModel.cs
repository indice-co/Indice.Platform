using IdentityServer4.Models;

namespace Indice.AspNetCore.Identity.Models;

/// <summary>View model for the error page</summary>
public class ErrorViewModel
{
    /// <summary>Creates a new <see cref="ErrorViewModel"/>.</summary>
    public ErrorViewModel() {
    }

    /// <summary>Creates a new <see cref="ErrorViewModel"/>.</summary>
    /// <param name="error">The error message</param>
    public ErrorViewModel(string error) {
        Error = new ErrorMessage { Error = error };
    }

    /// <summary>The error message</summary>
    public ErrorMessage Error { get; set; }
}
