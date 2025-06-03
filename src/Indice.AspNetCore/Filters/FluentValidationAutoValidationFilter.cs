using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Filters;

/// <summary>
/// An action filter that performs automatic FluentValidation validation on action parameters
/// based on the configured <see cref="AutoValidationMvcConfiguration"/>.
/// </summary>
public class FluentValidationAutoValidationActionFilter : IAsyncActionFilter, IAsyncPageFilter
{
    private readonly AutoValidationMvcConfiguration autoValidationMvcConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationAutoValidationActionFilter"/> class.
    /// </summary>
    /// <param name="autoValidationMvcConfiguration">The configuration options for auto validation.</param>
    public FluentValidationAutoValidationActionFilter(IOptions<AutoValidationMvcConfiguration> autoValidationMvcConfiguration) {
        this.autoValidationMvcConfiguration = autoValidationMvcConfiguration.Value;
    }

    /// <inheritdoc/>
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

                HandleUnvalidatedEntries(context);

                await next();

                return;
            }

            // Iterate through action parameters and perform validation if a validator is found.
            foreach (var parameter in controllerActionDescriptor.Parameters) {
                if (!context.ActionArguments.TryGetValue(parameter.Name, out var subject)) {
                    continue;
                }

                var parameterType = subject?.GetType();
                var bindingSource = parameter.BindingInfo?.BindingSource;
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

            // Mark unvalidated entries as skipped if DataAnnotations validation is disabled.
            HandleUnvalidatedEntries(context);

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


    /// <inheritdoc/>
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) {
        return Task.CompletedTask;
    }
    /// <inheritdoc/>
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next) {
        if (IsValidPageModel(context.HandlerInstance)) {
            var endpoint = context.HttpContext.GetEndpoint();
            var actionDescriptor = context.ActionDescriptor;
            var serviceProvider = context.HttpContext.RequestServices;

            // Skip validation if the endpoint is decorated with AutoValidationAttribute or AutoValidateNeverAttribute, depending on the strategy.
            if (endpoint != null &&
                (
                (autoValidationMvcConfiguration.ValidationStrategy == ValidationStrategy.Annotations && !endpoint.Metadata.OfType<AutoValidationAttribute>().Any()) ||
                 endpoint.Metadata.OfType<AutoValidateNeverAttribute>().Any())) {

                HandleUnvalidatedEntries(context);

                await next();

                return;
            }
            foreach (PageBoundPropertyDescriptor property in actionDescriptor.BoundProperties) {
                
                var parameterType = property.ParameterType;
                var bindingSource = property.BindingInfo?.BindingSource;
                var subject = property.Property.GetValue(context.HandlerInstance);
                if (subject != null && parameterType != null &&
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
            // Iterate through action parameters and perform validation if a validator is found.
            foreach (var parameter in actionDescriptor.Parameters) {
                if (!context.HandlerArguments.TryGetValue(parameter.Name, out var subject)) {
                    continue;
                }

                var parameterType = subject?.GetType();
                var bindingSource = parameter.BindingInfo?.BindingSource;
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

            // Mark unvalidated entries as skipped if DataAnnotations validation is disabled.
            HandleUnvalidatedEntries(context);
        }

        await next();
    }

    /// <summary>
    /// Marks unvalidated model state entries as skipped if DataAnnotations validation is disabled.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    private void HandleUnvalidatedEntries(ActionExecutingContext context) {
        if (autoValidationMvcConfiguration.DisableDataAnnotationsValidation) {
            foreach (var modelStateEntry in context.ModelState.Values.Where(modelStateEntry => modelStateEntry.ValidationState == ModelValidationState.Unvalidated)) {
                modelStateEntry.ValidationState = ModelValidationState.Skipped;
            }
        }
    }
    /// <summary>
    /// Marks unvalidated model state entries as skipped if DataAnnotations validation is disabled.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    private void HandleUnvalidatedEntries(PageHandlerExecutingContext context) {
        if (autoValidationMvcConfiguration.DisableDataAnnotationsValidation) {
            foreach (var modelStateEntry in context.ModelState.Values.Where(modelStateEntry => modelStateEntry.ValidationState == ModelValidationState.Unvalidated)) {
                modelStateEntry.ValidationState = ModelValidationState.Skipped;
            }
        }
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
    /// Determines if the given page model is a valid target for validation.
    /// </summary>
    /// <param name="pageModel">The page model instance.</param>
    /// <returns>True if the page model is valid for validation; otherwise, false.</returns>
    private static bool IsValidPageModel(object pageModel) {
        var modelType = pageModel.GetType();

        if (HasCustomAttribute<NonControllerAttribute>(modelType)) {
            return false;
        }
        return pageModel is PageModel;
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
    /// Disables the built-in .NET model (data annotations) validation.
    /// </summary>
    public bool DisableDataAnnotationsValidation { get; set; }

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

        // Register a custom object model validator if DataAnnotations validation is disabled.
        if (configuration.DisableDataAnnotationsValidation) {
            serviceCollection.AddSingleton<IObjectModelValidator, FluentValidationAutoValidationObjectModelValidator>(serviceProvider =>
                new FluentValidationAutoValidationObjectModelValidator(
                    serviceProvider.GetRequiredService<IModelMetadataProvider>(),
                    serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Value.ModelValidatorProviders,
                    configuration.DisableDataAnnotationsValidation));
        }

        // Create a default instance of the `ModelStateInvalidFilter` to access the non static property `Order` in a static context.
        var modelStateInvalidFilter = new ModelStateInvalidFilter(new ApiBehaviorOptions { InvalidModelStateResponseFactory = context => new OkResult() }, NullLogger.Instance);

        // Make sure we insert the `FluentValidationAutoValidationActionFilter` before the built-in `ModelStateInvalidFilter` to prevent it short-circuiting the request.
        serviceCollection.Configure<MvcOptions>(options => options.Filters.Add<FluentValidationAutoValidationActionFilter>(modelStateInvalidFilter.Order - 1));
        return serviceCollection;
    }

    /// <summary>
    /// Custom object model validator that can disable built-in model validation.
    /// </summary>
    public class FluentValidationAutoValidationObjectModelValidator : ObjectModelValidator
    {
        private readonly bool disableBuiltInModelValidation;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentValidationAutoValidationObjectModelValidator"/> class.
        /// </summary>
        /// <param name="modelMetadataProvider">The model metadata provider.</param>
        /// <param name="validatorProviders">The model validator providers.</param>
        /// <param name="disableBuiltInModelValidation">Whether to disable built-in model validation.</param>
        public FluentValidationAutoValidationObjectModelValidator(IModelMetadataProvider modelMetadataProvider, IList<IModelValidatorProvider> validatorProviders, bool disableBuiltInModelValidation)
            : base(modelMetadataProvider, validatorProviders) {
            this.disableBuiltInModelValidation = disableBuiltInModelValidation;
        }

        /// <summary>
        /// Gets a custom validation visitor that can skip built-in model validation.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <param name="validatorProvider">The validator provider.</param>
        /// <param name="validatorCache">The validator cache.</param>
        /// <param name="metadataProvider">The metadata provider.</param>
        /// <param name="validationState">The validation state dictionary.</param>
        /// <returns>A <see cref="ValidationVisitor"/> instance.</returns>
        public override ValidationVisitor GetValidationVisitor(ActionContext actionContext,
            IModelValidatorProvider validatorProvider,
            ValidatorCache validatorCache,
            IModelMetadataProvider metadataProvider,
            ValidationStateDictionary? validationState) {
            return new FluentValidationAutoValidationValidationVisitor(actionContext, validatorProvider, validatorCache, metadataProvider, validationState, disableBuiltInModelValidation);
        }
    }

    /// <summary>
    /// Custom validation visitor that can skip built-in model validation if configured.
    /// </summary>
    public class FluentValidationAutoValidationValidationVisitor : ValidationVisitor
    {
        private readonly bool disableBuiltInModelValidation;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentValidationAutoValidationValidationVisitor"/> class.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <param name="validatorProvider">The validator provider.</param>
        /// <param name="validatorCache">The validator cache.</param>
        /// <param name="metadataProvider">The metadata provider.</param>
        /// <param name="validationState">The validation state dictionary.</param>
        /// <param name="disableBuiltInModelValidation">Whether to disable built-in model validation.</param>
        public FluentValidationAutoValidationValidationVisitor(ActionContext actionContext,
            IModelValidatorProvider validatorProvider,
            ValidatorCache validatorCache,
            IModelMetadataProvider metadataProvider,
            ValidationStateDictionary? validationState,
            bool disableBuiltInModelValidation)
            : base(actionContext, validatorProvider, validatorCache, metadataProvider, validationState) {
            this.disableBuiltInModelValidation = disableBuiltInModelValidation;
        }

        /// <summary>
        /// Validates the model. If built-in model validation is disabled, always returns true to skip validation.
        /// </summary>
        /// <param name="metadata">The model metadata.</param>
        /// <param name="key">The model key.</param>
        /// <param name="model">The model instance.</param>
        /// <param name="alwaysValidateAtTopLevel">Whether to always validate at the top level.</param>
        /// <returns>True if validation should proceed; otherwise, false.</returns>
        public override bool Validate(ModelMetadata? metadata, string? key, object? model, bool alwaysValidateAtTopLevel) {
            // If built in model validation is disabled return true for later validation in the action filter.
            return disableBuiltInModelValidation || base.Validate(metadata, key, model, alwaysValidateAtTopLevel);
        }
    }
}