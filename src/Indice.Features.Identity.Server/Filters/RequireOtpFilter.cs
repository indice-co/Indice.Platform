using System.Security.Claims;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.AspNetCore.Http.RequireOtpFilterExtensions;

namespace Microsoft.AspNetCore.Http;

/// <summary>Endpoint require OTP extensions.</summary>
public static class RequireOtpFilterExtensions
{
    /// <summary>The default header value for capturing the TOTP code.</summary>
    public const string DEFAULT_HEADER_NAME = "X-TOTP";

    /// <summary>Marks the method as requiring an OTP.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">the builder.</param>
    /// <param name="headerName">The name of the header that contains the TOTP code.</param>
    /// <returns>The builder.</returns>
    public static TBuilder RequireOtp<TBuilder>(this TBuilder builder, string headerName) where TBuilder : IEndpointConventionBuilder =>
        RequireOtp(builder, (policy) => policy.FromHeader(headerName));

    /// <summary>Marks the method as requiring an OTP.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="configureAction">Configure action.</param>
    /// <returns>The builder.</returns>
    public static TBuilder RequireOtp<TBuilder>(this TBuilder builder, Action<RequireOtpPolicy>? configureAction = null) where TBuilder : IEndpointConventionBuilder {
        builder.Add(endpointBuilder => {
            // We can respond with problem details if there's a validation error.
            endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(HttpValidationProblemDetails), ["application/problem+json"]));
            endpointBuilder.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async (invocationContext) => {
                    var httpContext = invocationContext.HttpContext;
                    var principal = httpContext.User;
                    var policy = new RequireOtpPolicy();
                    configureAction?.Invoke(policy);
                    if (principal is null || !principal.Identity!.IsAuthenticated) {
                        return Results.ValidationProblem(ValidationErrors.AddError("Forbidden", "Authenticated user is required"), detail: "Principal is not present or not authenticated.");
                    }
                    // Check if user has an elevated access token and is already TOTP authenticated.
                    var isOtpAuthenticated = principal.FindFirstValue<bool>(BasicClaimTypes.OtpAuthenticated) ?? false;
                    if (isOtpAuthenticated) {
                        return await next(invocationContext);
                    }
                    var subject = principal.FindSubjectId();
                    if (string.IsNullOrWhiteSpace(subject)) {
                        return Results.ValidationProblem(ValidationErrors.AddError(BasicClaimTypes.Subject, "Missing subject"), detail: "A subject does not exist in user claims.");
                    }
                    var phoneNumber = principal.FindFirst(BasicClaimTypes.PhoneNumber)?.Value;
                    if (string.IsNullOrWhiteSpace(phoneNumber)) {
                        return Results.ValidationProblem(ValidationErrors.AddError(BasicClaimTypes.PhoneNumber, "Missing phone number"), detail: "A phone number does not exist in user claims.");
                    }
                    var userState = await (policy.GetUserState?.Invoke(httpContext) ?? Task.FromResult<object?>(null));
                    var errors = policy.Validate?.Invoke(httpContext.RequestServices, principal, userState);
                    if (errors?.Count > 0) {
                        return Results.ValidationProblem(errors, detail: errors.First().Value[0]);
                    }
                    var totpServiceFactory = httpContext.RequestServices.GetRequiredService<TotpServiceFactory>();
                    var totpService = totpServiceFactory.Create<User>();
                    var totp = httpContext.Request.Headers[policy.HeaderName].ToString();
                    var purpose = policy.ResolvePurpose(httpContext.RequestServices, principal, subject, phoneNumber, userState);
                    // No TOTP is present in the request, so will try to send one using the preferred delivery channel.
                    if (string.IsNullOrWhiteSpace(totp)) {
                        var deliveryChannel = TotpDeliveryChannel.Sms; //_serviceProvider.GetRequiredService<IOptions<DeviceOptions>>().Value.DefaultTotpDeliveryChannel;
                        await totpService.SendAsync(totp =>
                            totp.ToPrincipal(principal)
                                .WithMessage(policy.ResolveMessageTemplate(httpContext.RequestServices, principal, userState))
                                .UsingDeliveryChannel(deliveryChannel)
                                .WithPurpose(purpose)
                        );
                        return Results.ValidationProblem(ValidationErrors.AddError("requiresOtp", "Invalid totp"), detail: "The TOTP code could not be verified.", extensions: new Dictionary<string, object?> { ["requiresOtp"] = true });
                    }
                    return await next(invocationContext);
                });
            });
        });
        return builder;
    }

    /// <summary>Constructs the TOTP message to be sent.</summary>
    public delegate string GetTotpPurpose(IServiceProvider serviceProvider, ClaimsPrincipal principal, string subject, string phoneNumber, object? userState);
    /// <summary>Constructs the TOTP message to be sent.</summary>
    public delegate string GetTotpMessageTemplate(IServiceProvider serviceProvider, ClaimsPrincipal principal, object? userState);
    /// <summary>Perform custom validation</summary>
    public delegate IDictionary<string, string[]>? Validate(IServiceProvider serviceProvider, ClaimsPrincipal principal, object? userState);
    /// <summary>Gets state for current user</summary>
    public delegate Task<object?> GetUserState(HttpContext httpContext);
}

/// <summary>Policy configuration for <see cref="RequireOtpFilterExtensions.RequireOtp{TBuilder}(TBuilder, string)"/> filter.</summary>
public sealed class RequireOtpPolicy
{
    /// <summary>Configure custom message template.</summary>
    public RequireOtpPolicy AddMessageTemplate(GetTotpMessageTemplate action) {
        ResolveMessageTemplate = action;
        return this;
    }

    /// <summary>Configure custom purpose.</summary>
    public RequireOtpPolicy AddPurpose(GetTotpPurpose action) {
        ResolvePurpose = action;
        return this;
    }

    /// <summary>Configure user state.</summary>
    public RequireOtpPolicy AddState(GetUserState action) {
        GetUserState = action;
        return this;
    }

    /// <summary>Configure validator.</summary>
    public RequireOtpPolicy AddValidator(Validate action) {
        Validate = action;
        return this;
    }

    /// <summary>Adds the HTTP header to check for the OTP.</summary>
    /// <remarks>Defaults to <strong>X-TOTP</strong>.</remarks>
    public RequireOtpPolicy FromHeader(string headerName) {
        HeaderName = headerName;
        return this;
    }

    /// <summary>The name of the header that contains the TOTP code.</summary>
    public string HeaderName { get; set; } = DEFAULT_HEADER_NAME;

    /// <summary>Determines how the TOTP message is created.</summary>
    public GetTotpPurpose ResolvePurpose { get; private set; } = (servicePrincipal, principal, subject, phoneNumber, state) => $"{nameof(RequireOtpPolicy)}:{subject}:{phoneNumber}";

    /// <summary>Retrieves the TOTP message template. Defaults to RequiresOtpMessage </summary>
    public GetTotpMessageTemplate ResolveMessageTemplate { get; private set; } = (servicePrincipal, principal, state) => servicePrincipal.GetRequiredService<IdentityMessageDescriber>().RequiresOtpMessage();

    /// <summary>Retrieves the TOTP message template. Defaults to RequiresOtpMessage </summary>
    public GetUserState? GetUserState { get; private set; }

    /// <summary>Perform custom validation.</summary>
    public Validate? Validate { get; private set; }
}
