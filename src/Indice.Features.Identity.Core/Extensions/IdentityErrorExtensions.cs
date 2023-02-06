using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.Core.Extensions
{
    /// <summary>Extension methods for <see cref="IdentityError"/> type.</summary>
    public static class IdentityErrorExtensions
    {
        /// <summary>Converts a list of <see cref="IdentityError"/> objects to <see cref="ValidationProblemDetails"/>.</summary>
        /// <param name="errors">The list of <see cref="IdentityError"/> occurred.</param>
        public static ValidationProblemDetails ToValidationProblemDetails(this IEnumerable<IdentityError> errors) => 
            new(errors.ToDictionary(x => x.Code, x => new[] { x.Description }));
    }
}
