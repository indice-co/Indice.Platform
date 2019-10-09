using System;
using System.Text.RegularExpressions;
using FluentValidation;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Extension methods on <see cref="IRuleBuilder{T, TProperty}"/>.
    /// </summary>
    internal static class ValidatorExtensions
    {
        /// <summary>
        /// Validates a string for being a valid regular expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ruleBuilder">Rule builder.</param>
        public static IRuleBuilderOptions<T, string> RegularExpression<T>(this IRuleBuilder<T, string> ruleBuilder) => ruleBuilder.Must(BeValidRegularExpression);

        private static bool BeValidRegularExpression(string expression) {
            var isValid = true;
            if (expression != null && expression.Trim().Length > 0) {
                try {
                    Regex.Match(string.Empty, expression);
                } catch (ArgumentException) {
                    isValid = false;
                }
            } else {
                isValid = false;
            }
            return isValid;
        }
    }
}
