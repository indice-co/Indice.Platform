using System;
using System.Threading.Tasks;
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
        /// <summary>The name of the header that contains the TOTP code.</summary>
        public string OtpHeaderName { get; set; } = "X-TOTP";
        /// <summary>The chosen delivery channel used to send the TOTP code.</summary>
        public TotpDeliveryChannel DeliveryChannel { get; set; } = TotpDeliveryChannel.Sms;
        /// <summary>The name of the header that contains the chosen delivery channel. If header is set it takes precedence over the <see cref="DeliveryChannel"/> property.</summary>
        public string DeliveryChannelHeaderName { get; set; } = "X-Delivery-Channel";

        /// <inheritdoc />
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            var httpContext = context.HttpContext;
            var principal = httpContext.User;
            if (principal is null || !principal.Identity.IsAuthenticated) {
                throw new OtpException("Principal is not present or not authenticated.");
            }
            // Check if user has an elevated access token and is already TOTP authenticated.
            var isOtpAuthenticated = httpContext.User.FindFirstValue<bool>(BasicClaimTypes.OtpAuthenticated) ?? false;
            if (isOtpAuthenticated) {
                await next();
                return;
            }
            var serviceProvicer = httpContext.RequestServices;
            var totpService = serviceProvicer.GetRequiredService<ITotpService>();
            var messageDescriber = serviceProvicer.GetRequiredService<IdentityMessageDescriber>();
            var totp = httpContext.Request.Headers[OtpHeaderName];
            var purpose = $"{nameof(RequiresOtpAttribute)}:";
            // No TOTP is present in the request, so will try to send one using the preferred delivery channel.
            if (string.IsNullOrWhiteSpace(totp)) {
                await totpService.Send(builder =>
                    builder.UsePrincipal(principal)
                           .WithMessage("")
                           .UsingDeliveryChannel(DeliveryChannel)
                           .WithPurpose("")
                );
                return;
            }
        }
    }

    /// <summary>Used to propagate exceptions when <see cref="RequiresOtpAttribute"/> is used.</summary>
    public class OtpException : BusinessException
    {
        /// <summary>Creates a new instance of <see cref="OtpException"/> with the specified error message.</summary>
        /// <param name="message">The message that describes the error.</param>
        public OtpException(string message) : base(message) { }
    }
}
