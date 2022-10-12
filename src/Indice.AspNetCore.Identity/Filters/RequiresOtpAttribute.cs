using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel;
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
            var serviceProvicer = httpContext.RequestServices;
            var totpService = serviceProvicer.GetRequiredService<ITotpService>();
            var messageDescriber = serviceProvicer.GetRequiredService<IdentityMessageDescriber>();
            var totp = httpContext.Request.Headers[HeaderName].ToString();
            var purpose = $"{nameof(RequiresOtpAttribute)}:{subject}:{phoneNumber}";
            // No TOTP is present in the request, so will try to send one using the preferred delivery channel.
            if (string.IsNullOrWhiteSpace(totp)) {
                await totpService.Send(builder =>
                    builder.UsePrincipal(principal)
                           .WithMessage(messageDescriber.RequiresOtpMessage())
                           .UsingDeliveryChannel(DeliveryChannel)
                           .WithPurpose(purpose)
                );
                throw new BusinessException("TOTP is required.", "totp", new List<string> { "An TOTP code is required to call this endpoint." });
            }
            // If a TOTP exists in the request, then we need to verify it.
            var totpResult = await totpService.Verify(principal, totp, purpose: purpose);
            if (!totpResult.Success) {
                throw new BusinessException("TOTP not required.", "totp", new List<string> { "The TOTP code could not be verified." });
            }
            await next();
        }
    }
}
