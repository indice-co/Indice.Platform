using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Filters;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity.Api.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class TrustDeviceRequiresOtpAttribute : RequiresOtpAttribute, IAsyncActionFilter
    {
        private IServiceProvider _serviceProvider;
        private UserDevice _device;

        public new async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            var deviceIdSpecified = context.RouteData.Values.TryGetValue("deviceId", out var deviceId);
            if (deviceIdSpecified) {
                var httpContext = context.HttpContext;
                var principal = httpContext.User;
                if (principal is null || !principal.Identity.IsAuthenticated) {
                    throw new BusinessException("Principal is not present or not authenticated.");
                }
                _serviceProvider = httpContext.RequestServices;
                var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
                var user = await userManager.GetUserAsync(principal);
                if (user is null) {
                    var problemDetails = new ValidationProblemDetails {
                        Detail = "User does not exist.",
                        Status = StatusCodes.Status400BadRequest
                    };
                    context.Result = new BadRequestObjectResult(problemDetails);
                    return;
                }
                _device = await userManager.GetDeviceByIdAsync(user, deviceId.ToString());
                if (_device is null) {
                    var problemDetails = new ValidationProblemDetails {
                        Detail = "Device does not exist.",
                        Status = StatusCodes.Status400BadRequest
                    };
                    context.Result = new BadRequestObjectResult(problemDetails);
                    return;
                }
            }
            await base.OnActionExecutionAsync(context, next);
        }

        protected override string GetTotpMessage() {
            var messageDescriber = _serviceProvider.GetRequiredService<IdentityMessageDescriber>();
            return messageDescriber.TrustedDeviceRequiresOtpMessage(_device);
        }

        protected override string GetTotpPurpose(string subject, string phoneNumber) {
            var purpose = $"{nameof(TrustDeviceRequiresOtpAttribute)}:{subject}:{phoneNumber}:{_device.Id}";
            return purpose;
        }
    }
}
