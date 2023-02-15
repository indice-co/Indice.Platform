using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity
{
    /// <summary>Wraps account controller operations regarding creating and validating view models.</summary>
    public interface IAccountService
    {
        /// <summary>Builds the <see cref="LoginViewModel"/>.</summary>
        /// <param name="returnUrl">The return URL to go to after successful login.</param>
        Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl);
        /// <summary>Builds the <see cref="MfaLoginViewModel"/>.</summary>
        /// <param name="returnUrl">The return URL to go to after successful login.</param>
        /// <param name="downgradeMfaChannel">Chooses a less secure channel for MFA, if possible.</param>
        /// <param name="mfaChannel">The MFA channel to use if <paramref name="downgradeMfaChannel"/> is set to true or no trusted devices exist.</param>
        Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(string returnUrl, bool downgradeMfaChannel = false, TotpDeliveryChannel? mfaChannel = null);
        /// <summary>Generic counterpart in case someone extends the basic <see cref="RegisterViewModel"/> with extra properties.</summary>
        /// <typeparam name="TRegisterViewModel">The type of <see cref="RegisterViewModel"/>.</typeparam>
        /// <param name="returnUrl">The return URL.</param>
        Task<TRegisterViewModel> BuildRegisterViewModelAsync<TRegisterViewModel>(string returnUrl) where TRegisterViewModel : RegisterViewModel, new();
        /// <summary>Builds the logout view model.</summary>
        /// <param name="logoutId">The logout id.</param>
        Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId);
        /// <summary>Build the post logout view model. <see cref="LoggedOutViewModel"/>.</summary>
        /// <param name="logoutId">The logout id.</param>
        Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId);
    }

    /// <summary>Extension methods on <see cref="IAccountService"/>.</summary>
    public static class IAccountServiceExtensions
    {
        /// <summary>Builds the <see cref="LoginViewModel"/> from the posted request <see cref="LoginInputModel"/>.</summary>
        /// <param name="accountService">The <see cref="IAccountService"/> instance.</param>
        /// <param name="model">The request model.</param>
        public static async Task<LoginViewModel> BuildLoginViewModelAsync(this IAccountService accountService, LoginInputModel model) {
            var viewModel = await accountService.BuildLoginViewModelAsync(model.ReturnUrl);
            viewModel.UserName = model.UserName;
            viewModel.RememberLogin = model.RememberLogin;
            return viewModel;
        }

        /// <summary>Builds the <see cref="MfaLoginViewModel"/> from the posted request <see cref="MfaLoginInputModel"/>.</summary>
        /// <param name="accountService">The <see cref="IAccountService"/> instance.</param>
        /// <param name="model">The request model.</param>
        public static async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(this IAccountService accountService, MfaLoginInputModel model) {
            var viewModel = await accountService.BuildMfaLoginViewModelAsync(model.ReturnUrl);
            viewModel.OtpCode = null;
            viewModel.RememberClient = model.RememberClient;
            viewModel.RememberMe = model.RememberMe;
            return viewModel;
        }

        /// <summary>Builds the <see cref="RegisterViewModel"/>.</summary>
        /// <param name="accountService">The <see cref="IAccountService"/> instance.</param>
        /// <param name="returnUrl">The return URL to go to after successful login.</param>
        public static async Task<RegisterViewModel> BuildRegisterViewModelAsync(this IAccountService accountService, string returnUrl) =>
            await accountService.BuildRegisterViewModelAsync<RegisterViewModel>(returnUrl);

        /// <summary>Builds the <see cref="RegisterViewModel"/> from the posted request <see cref="RegisterRequest"/>. Generic counterpart in case someone extends the basic <see cref="RegisterViewModel"/> with extra properties.</summary>
        /// <typeparam name="TRegisterViewModel">The type of <see cref="RegisterViewModel"/>.</typeparam>
        /// <param name="accountService">The <see cref="IAccountService"/> instance.</param>
        /// <param name="model">The request model.</param>
        public static async Task<TRegisterViewModel> BuildRegisterViewModelAsync<TRegisterViewModel>(this IAccountService accountService, RegisterRequest model) where TRegisterViewModel : RegisterViewModel, new() {
            var viewModel = await accountService.BuildRegisterViewModelAsync<TRegisterViewModel>(model.ReturnUrl);
            viewModel.UserName = model.UserName;
            viewModel.FirstName = model.FirstName;
            viewModel.LastName = model.LastName;
            viewModel.PhoneNumber = model.PhoneNumber;
            viewModel.Password = model.Password;
            return viewModel;
        }

        /// <summary>Builds the <see cref="RegisterViewModel"/> from the posted request <see cref="RegisterRequest"/>.</summary>
        /// <param name="accountService">The <see cref="IAccountService"/> instance.</param>
        /// <param name="model">The request model.</param>
        public static async Task<RegisterViewModel> BuildRegisterViewModelAsync(this IAccountService accountService, RegisterRequest model) =>
            await accountService.BuildRegisterViewModelAsync<RegisterViewModel>(model);
    }
}
