using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using Humanizer;
using Microsoft.AspNetCore.Mvc;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Used to provide hooks that will be called before and after MVC validation occurs.
    /// </summary>
    public class ValidatorInterceptor : IValidatorInterceptor
    {
        /// <summary>
        /// Invoked after MVC validation takes place which allows the result to be customized. It should return a ValidationResult.
        /// </summary>
        /// <param name="controllerContext">The context associated with the current request for a controller.</param>
        /// <param name="validationContext">Validation context.</param>
        /// <param name="result">The result of running a validator.</param>
        public ValidationResult AfterMvcValidation(ControllerContext controllerContext, ValidationContext validationContext, ValidationResult result) {
            if (!result.IsValid) {
                foreach (var error in result.Errors) {
                    var propertyName = error.PropertyName.Camelize();
                    error.PropertyName = propertyName;
                }
            }
            return result;
        }

        /// <summary>
        /// Invoked before MVC validation takes place which allows the ValidationContext to be customized prior to validation. It should return a ValidationContext object.
        /// </summary>
        /// <param name="controllerContext">The context associated with the current request for a controller.</param>
        /// <param name="validationContext">Validation context.</param>
        public ValidationContext BeforeMvcValidation(ControllerContext controllerContext, ValidationContext validationContext) => validationContext;
    }
}
