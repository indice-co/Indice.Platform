using Indice.Services;

namespace Indice.AspNetCore.Identity
{
    /// <summary><see cref="TotpServiceBase"/> result.</summary>
    public class TotpResult
    {
        /// <summary>Constructs an error result.</summary>
        /// <param name="error">The error.</param>
        public static TotpResult ErrorResult(string error) => new() {
            Error = error
        };

        /// <summary>Indicates success.</summary>
        public bool Success { get; private set; }
        /// <summary>The error occurred.</summary>
        public string Error { get; private set; }

        /// <summary>Constructs a success result.</summary>
        public static TotpResult SuccessResult => new() {
            Success = true 
        };
    }
}
