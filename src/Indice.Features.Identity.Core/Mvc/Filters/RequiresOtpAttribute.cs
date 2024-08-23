using IdentityModel;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Core.Mvc.Filters;

/// <summary>An attribute that can be applied to action methods that require OTP verification.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RequiresOtpAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>The default header value for capturing the TOTP code.</summary>
    public const string DEFAULT_HEADER_NAME = "X-TOTP";
    /// <summary>The name of the header that contains the TOTP code.</summary>
    public string HeaderName { get; set; } = DEFAULT_HEADER_NAME;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var httpContext = context.HttpContext;
        var principal = httpContext.User;
        if (principal.Identity is null || !principal.Identity.IsAuthenticated) {
            var problemDetails = new ValidationProblemDetails {
                Detail = "Principal is not present or not authenticated.",
                Status = StatusCodes.Status400BadRequest
            };
            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }
        // Check if user has an elevated access token and is already TOTP authenticated.
        var isOtpAuthenticated = principal.FindFirstValue<bool>(CustomGrantTypes.Mfa) ?? false;
        if (isOtpAuthenticated) {
            await next();
            return;
        }
        var subject = principal.FindSubjectId();
        if (string.IsNullOrWhiteSpace(subject)) {
            var problemDetails = new ValidationProblemDetails {
                Detail = "A subject does not exist in user claims.",
                Status = StatusCodes.Status400BadRequest
            };
            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }
        var phoneNumber = principal.FindFirst(JwtClaimTypes.PhoneNumber)?.Value;
        if (string.IsNullOrWhiteSpace(phoneNumber)) {
            var problemDetails = new ValidationProblemDetails {
                Detail = "A phone number does not exist in user claims.",
                Status = StatusCodes.Status400BadRequest
            };
            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }
        var totpServiceFactory = httpContext.RequestServices.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create<User>();
        var totp = httpContext.Request.Headers[HeaderName].ToString();
        var purpose = GetTotpPurpose(subject, phoneNumber);
        // No TOTP is present in the request, so will try to send one using the preferred delivery channel.
        if (string.IsNullOrWhiteSpace(totp)) {
            var deliveryChannel = TotpDeliveryChannel.Sms;
            await totpService.SendAsync(totp =>
                totp.ToPrincipal(principal)
                    .WithMessage(GetTotpMessage(httpContext.RequestServices))
                    .UsingDeliveryChannel(deliveryChannel)
                    .WithPurpose(purpose)
            );
            var problemDetails = new ValidationProblemDetails {
                Detail = "An TOTP code is required to call this endpoint.",
                Status = StatusCodes.Status400BadRequest
            };
            problemDetails.Extensions.Add("requiresOtp", true);
            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }
        // If a TOTP exists in the request, then we need to verify it.
        var totpResult = await totpService.VerifyAsync(principal, totp, purpose);
        if (!totpResult.Success) {
            var problemDetails = new ValidationProblemDetails {
                Detail = "The TOTP code could not be verified.",
                Status = StatusCodes.Status400BadRequest
            };
            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }
        await next();
    }

    /// <summary>Constructs the TOTP message to be sent.</summary>
    protected virtual string GetTotpMessage(IServiceProvider serviceProvider) {
        var messageDescriber = serviceProvider.GetRequiredService<IdentityMessageDescriber>();
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
