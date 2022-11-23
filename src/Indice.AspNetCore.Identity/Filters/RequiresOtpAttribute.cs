using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity.Filters
{
    /// <summary>An attribute that can be applied to action methods that require OTP verification.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequiresOtpAttribute : Attribute, IAsyncActionFilter
    {
        private IServiceProvider _serviceProvider;

        /// <summary>The default header value for capturing the TOTP code.</summary>
        public const string DEFAULT_HEADER_NAME = "X-TOTP";
        /// <summary>The name of the header that contains the TOTP code.</summary>
        public string HeaderName { get; set; } = DEFAULT_HEADER_NAME;
        /// <summary>The chosen delivery channel used to send the TOTP code.</summary>
        public TotpDeliveryChannel DeliveryChannel { get; set; } = TotpDeliveryChannel.Sms;

        /// <inheritdoc />
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            var httpContext = context.HttpContext;
            var principal = httpContext.User;
            if (principal is null || !principal.Identity.IsAuthenticated) {
                throw new BusinessException("Principal is not present or not authenticated.");
            }
            // Check if user has an elevated access token and is already TOTP authenticated.
            var isOtpAuthenticated = principal.FindFirstValue<bool>(BasicClaimTypes.OtpAuthenticated) ?? false;
            if (isOtpAuthenticated) {
                await next();
                return;
            }
            var subject = principal.FindSubjectId();
            if (string.IsNullOrWhiteSpace(subject)) {
                throw new BusinessException("A subject does not exist in user claims.");
            }
            var phoneNumber = principal.FindFirst(JwtClaimTypes.PhoneNumber)?.Value;
            if (string.IsNullOrWhiteSpace(phoneNumber)) {
                throw new BusinessException("A phone number does not exist in user claims.");
            }
            _serviceProvider = httpContext.RequestServices;
            var totpServiceFactory = _serviceProvider.GetRequiredService<TotpServiceFactory>();
            var totpService = totpServiceFactory.Create<User>();
            var totp = httpContext.Request.Headers[HeaderName].ToString();
            var purpose = GetTotpPurpose(subject, phoneNumber);
            // No TOTP is present in the request, so will try to send one using the preferred delivery channel.
            if (string.IsNullOrWhiteSpace(totp)) {
                await totpService.SendAsync(totp =>
                    totp.ToPrincipal(principal)
                        .WithMessage(GetTotpMessage())
                        .UsingDeliveryChannel(DeliveryChannel)
                        .WithPurpose(purpose)
                );
                throw new BusinessException("TOTP is required.", "requiresTotp", new List<string> { "An TOTP code is required to call this endpoint." });
            }
            // If a TOTP exists in the request, then we need to verify it.
            var totpResult = await totpService.VerifyAsync(principal, totp, purpose);
            if (!totpResult.Success) {
                throw new BusinessException("TOTP not verified.", "requiresTotp", new List<string> { "The TOTP code could not be verified." });
            }
            await next();
        }

        /// <summary>Constructs the TOTP message to be sent.</summary>
        protected virtual string GetTotpMessage() {
            var messageDescriber = _serviceProvider.GetRequiredService<IdentityMessageDescriber>();
            return messageDescriber.RequiresOtpMessage();
        }

        /// <summary>Constructs the TOTP message to be sent.</summary>
        /// <param name="subject">The subject of the user.</param>
        /// <param name="phoneNumber">The phone number of the user.</param>
        protected virtual string GetTotpPurpose(string subject, string phoneNumber) {
            var purpose = $"{nameof(RequiresOtpAttribute)}:{subject}:{phoneNumber}";
            return purpose;
        }
    }
}
