using System;
using FluentValidation;
using Humanizer;
using Indice.AspNetCore.Identity.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Identity.Models.Validators
{
    /// <summary>
    /// Validator for <see cref="ChangePasswordModel"/> model.
    /// </summary>
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordModel>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ChangePasswordValidator"/>.
        /// </summary>
        public ChangePasswordValidator() {
            RuleFor(x => x.NewPassword).NotEmpty().WithName(nameof(ChangePasswordModel.NewPassword).Humanize(LetterCasing.Sentence));
            RuleFor(x => x.OldPassword).NotEmpty().WithName(nameof(ChangePasswordModel.OldPassword).Humanize(LetterCasing.Sentence));
        }
    }

    /// <summary>
    /// Validator for <see cref="ForgotPasswordRequest"/> model.
    /// </summary>
    public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ForgotPasswordValidator"/>.
        /// </summary>
        public ForgotPasswordValidator() => RuleFor(x => x.Email).EmailAddress().NotEmpty();
    }

    /// <summary>
    /// Validator for <see cref="LoginViewModel"/> model.
    /// </summary>
    public class LoginValidator : AbstractValidator<LoginViewModel>
    {
        /// <summary>
        /// Creates a new instance of <see cref="LoginValidator"/>.
        /// </summary>
        public LoginValidator() {
            RuleFor(x => x.Password).NotEmpty().WithName(nameof(LoginViewModel.Password));
            RuleFor(x => x.Username).NotEmpty().WithName(nameof(LoginViewModel.Username));
        }
    }

    /// <summary>
    /// Validator for <see cref="RegisterViewModel"/> model.
    /// </summary>
    public class RegisterValidator : AbstractValidator<RegisterViewModel>
    {
        /// <summary>
        /// Creates a new instance of <see cref="RegisterValidator"/>.
        /// </summary>
        public RegisterValidator() {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithName(nameof(RegisterViewModel.Email));
            RuleFor(x => x.FirstName).NotEmpty().WithName(nameof(RegisterViewModel.FirstName).Humanize(LetterCasing.Sentence));
            RuleFor(x => x.LastName).NotEmpty().WithName(nameof(RegisterViewModel.LastName).Humanize(LetterCasing.Sentence));
            RuleFor(x => x.Password).NotEmpty().WithName(nameof(RegisterViewModel.Password));
        }
    }
}
