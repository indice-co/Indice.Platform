using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Filters;

/// <summary>
/// An action filter that performs automatic FluentValidation validation on action parameters
/// based on the configured <see cref="AutoValidationMvcConfiguration"/>.
/// </summary>
public class FluentValidationAutoValidationActionFilter : IAsyncActionFilter
{
    private readonly AutoValidationMvcConfiguration autoValidationMvcConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationAutoValidationActionFilter"/> class.
    /// </summary>
    /// <param name="autoValidationMvcConfiguration">The configuration options for auto validation.</param>
    public FluentValidationAutoValidationActionFilter(IOptions<AutoValidationMvcConfiguration> autoValidationMvcConfiguration) {
        this.autoValidationMvcConfiguration = autoValidationMvcConfiguration.Value;
    }

    /// <summary>
    /// Called asynchronously before the action, to perform FluentValidation validation on action parameters.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    /// <param name="next">The delegate to execute the next action filter or action.</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        if (IsValidController(context.Controller)) {
            var endpoint = context.HttpContext.GetEndpoint();
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            var serviceProvider = context.HttpContext.RequestServices;

            // Skip validation if the endpoint is decorated with AutoValidationAttribute or AutoValidateNeverAttribute, depending on the strategy.
            if (endpoint != null &&
                (
                (autoValidationMvcConfiguration.ValidationStrategy == ValidationStrategy.Annotations && !endpoint.Metadata.OfType<AutoValidationAttribute>().Any()) ||
                 endpoint.Metadata.OfType<AutoValidateNeverAttribute>().Any())) {

                await next();

                return;
            }

            // Iterate through action parameters and perform validation if a validator is found.
            foreach (var parameter in controllerActionDescriptor.Parameters) {
                if (context.ActionArguments.TryGetValue(parameter.Name, out var subject)) {
                    var parameterType = subject?.GetType();
                    var bindingSource = parameter.BindingInfo?.BindingSource;
                    var subjectNotNull = subject != null;
                    var pameterNotNull = parameterType != null;
                    var hasValidBindingSource = HasValidBindingSource(bindingSource);
                    var validatorRes = GetValidator(serviceProvider, parameterType);
                    if (subject != null && parameterType != null &&
                         HasValidBindingSource(bindingSource) &&
                        GetValidator(serviceProvider, parameterType) is IValidator validator) {
                        IValidationContext validationContext = new ValidationContext<object>(subject);
                        var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                        if (!validationResult.IsValid) {
                            foreach (var error in validationResult.Errors) {
                                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                            }
                        }
                    }
                }
            }

            // If model state is invalid, return a BadRequestObjectResult with validation details.
            if (!context.ModelState.IsValid) {
                var problemDetailsFactory = serviceProvider.GetRequiredService<ProblemDetailsFactory>();
                var validationProblemDetails = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);

                context.Result = new BadRequestObjectResult(validationProblemDetails);

                return;
            }
        }

        await next();
    }

    /// <summary>
    /// Determines if the given controller is a valid target for validation.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <returns>True if the controller is valid for validation; otherwise, false.</returns>
    private static bool IsValidController(object controller) {
        var controllerType = controller.GetType();

        if (HasCustomAttribute<NonControllerAttribute>(controllerType)) {
            return false;
        }

        return controller is ControllerBase ||
               HasCustomAttribute<ControllerAttribute>(controllerType) ||
               InheritsFromTypeWithNameEndingIn(controllerType, "Controller");
    }

    /// <summary>
    /// Checks if the binding source is valid for validation (Body, Form, or Query).
    /// </summary>
    /// <param name="bindingSource">The binding source.</param>
    /// <returns>True if the binding source is valid; otherwise, false.</returns>
    private static bool HasValidBindingSource(BindingSource? bindingSource) =>
        bindingSource == BindingSource.Body || bindingSource == BindingSource.Form || bindingSource == BindingSource.Query;

    /// <summary>
    /// Gets the validator for the specified type from the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="type">The type to validate.</param>
    /// <returns>The validator instance if found; otherwise, null.</returns>
    private static object? GetValidator(IServiceProvider serviceProvider, Type type) {
        return serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(type));
    }

    /// <summary>
    /// Checks if the specified type has a custom attribute of the given type.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the attribute is present; otherwise, false.</returns>
    private static bool HasCustomAttribute<TAttribute>(Type type) where TAttribute : Attribute {
        return type.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(TAttribute));
    }

    /// <summary>
    /// Determines if the type inherits from a type whose name ends with the specified string.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="name">The string the base type name should end with.</param>
    /// <returns>True if a base type name ends with the specified string; otherwise, false.</returns>
    private static bool InheritsFromTypeWithNameEndingIn(Type type, string name) {
        while (type.BaseType != null) {
            type = type.BaseType;

            if (type.Name.EndsWith(name, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Attribute to indicate that automatic validation should never be applied to the target.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
public class AutoValidateNeverAttribute : Attribute
{
}

/// <summary>
/// Attribute to indicate that automatic validation should be applied to the target.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AutoValidationAttribute : Attribute
{
}

/// <summary>
/// Configuration options for automatic FluentValidation validation in MVC.
/// </summary>
public class AutoValidationMvcConfiguration
{
    /// <summary>
    /// Configures the validation strategy. Validation strategy <see cref="ValidationStrategy.All"/> enables asynchronous automatic validation on all controllers inheriting from <see cref="ControllerBase"/>.
    /// Validation strategy <see cref="ValidationStrategy.Annotations"/> enables asynchronous automatic validation on controllers inheriting from <see cref="ControllerBase"/> decorated (class or method) with a <see cref="AutoValidationAttribute"/> attribute.
    /// </summary>
    public ValidationStrategy ValidationStrategy { get; set; } = ValidationStrategy.All;
}

/// <summary>
/// Specifies the strategy for automatic validation.
/// </summary>
public enum ValidationStrategy
{
    /// <summary>
    /// Enables asynchronous automatic validation on all controllers inheriting from <see cref="ControllerBase"/>.
    /// </summary>
    All = 1,

    /// <summary>
    /// Enables asynchronous automatic validation on controllers inheriting from <see cref="ControllerBase"/> decorated with a <see cref="AutoValidationAttribute"/> attribute.
    /// </summary>
    Annotations = 2
}

/// <summary>
/// Extension methods for registering FluentValidation automatic validation in MVC.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds asynchronous MVC Fluent Validation automatic validation to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="autoValidationMvcConfiguration">The configuration delegate used to configure the FluentValidation AutoValidation MVC validation.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddFluentValidationAutoValidation(this IServiceCollection serviceCollection, Action<AutoValidationMvcConfiguration>? autoValidationMvcConfiguration = null) {
        var configuration = new AutoValidationMvcConfiguration();

        if (autoValidationMvcConfiguration != null) {
            autoValidationMvcConfiguration.Invoke(configuration);
            serviceCollection.Configure(autoValidationMvcConfiguration);
        }
        // Create a default instance of the `ModelStateInvalidFilter` to access the non static property `Order` in a static context.
        var modelStateInvalidFilter = new ModelStateInvalidFilter(new ApiBehaviorOptions { InvalidModelStateResponseFactory = context => new OkResult() }, NullLogger.Instance);

        // Make sure we insert the `FluentValidationAutoValidationActionFilter` before the built-in `ModelStateInvalidFilter` to prevent it short-circuiting the request.
        serviceCollection.Configure<MvcOptions>(options => options.Filters.Add<FluentValidationAutoValidationActionFilter>(modelStateInvalidFilter.Order - 1));

        return serviceCollection;
    }
}