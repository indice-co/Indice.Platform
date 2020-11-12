using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Extension methods for <see cref="IdentityError"/> type.
    /// </summary>
    public static class IdentityErrorExtensions
    {
        /// <summary>
        /// Converts a list of <see cref="IdentityError"/> objects to <see cref="ValidationProblemDetails"/>.
        /// </summary>
        /// <param name="errors">The list of <see cref="IdentityError"/> occured.</param>
        public static ValidationProblemDetails AsValidationProblemDetails(this IEnumerable<IdentityError> errors) => 
            new ValidationProblemDetails(errors.ToDictionary(x => x.Code.Camelize(), x => new[] { x.Description }));
    }
}
