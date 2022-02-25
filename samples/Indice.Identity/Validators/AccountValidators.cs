using FluentValidation;
using Humanizer;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Models;

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
    /// Validator for <see cref="PasswordExpiredModel"/> model.
    /// </summary>
    public class PasswordExpiredValidator : AbstractValidator<PasswordExpiredModel>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ChangePasswordValidator"/>.
        /// </summary>
        public PasswordExpiredValidator() {
            RuleFor(x => x.NewPassword).NotEmpty().WithName(nameof(PasswordExpiredModel.NewPassword).Humanize(LetterCasing.Sentence));
            RuleFor(x => x.NewPasswordConfirmation).NotEmpty().WithName(nameof(PasswordExpiredModel.NewPasswordConfirmation).Humanize(LetterCasing.Sentence));
            RuleFor(x => x.NewPasswordConfirmation).Equal(x => x.NewPassword).WithName(nameof(PasswordExpiredModel.NewPasswordConfirmation).Humanize(LetterCasing.Sentence));
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
            RuleFor(x => x.UserName).NotEmpty().WithName(nameof(LoginViewModel.UserName));
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
