#if NET7_0_OR_GREATER
using System.Reflection;
using Indice.AspNetCore.Http.Validation;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MiniValidation;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Model state validation convention extensions</summary>
public static partial class ValidationFilterExtensions
{
    /// <summary>Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.</summary>
    /// <typeparam name="TParameter">The type to add input validation for</typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <returns>The builder.</returns>
    public static RouteGroupBuilder WithParameterValidation<TParameter>(this RouteGroupBuilder builder) => WithParameterValidation(builder, typeof(TParameter));

    /// <summary>Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.</summary>
    /// <typeparam name="TParameter">The type to add input validation for</typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <returns>The builder.</returns>
    public static RouteHandlerBuilder WithParameterValidation<TParameter>(this RouteHandlerBuilder builder) => WithParameterValidation(builder, typeof(TParameter));

    /// <summary>Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.</summary>
    /// <typeparam name="TParameter">The type to add input validation for</typeparam>
    /// <typeparam name="TParameter1">The type to add input validation for</typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <returns>The builder.</returns>
    public static RouteGroupBuilder WithParameterValidation<TParameter, TParameter1>(this RouteGroupBuilder builder) => WithParameterValidation(builder, typeof(TParameter), typeof(TParameter1));

    /// <summary>Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.</summary>
    /// <typeparam name="TParameter">The type to add input validation for.</typeparam>
    /// <typeparam name="TParameter1">The type to add input validation for.</typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <returns>The builder.</returns>
    public static RouteHandlerBuilder WithParameterValidation<TParameter, TParameter1>(this RouteHandlerBuilder builder) => WithParameterValidation(builder, typeof(TParameter), typeof(TParameter1));

    /// <summary>Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.</summary>
    /// <typeparam name="TParameter">The type to add input validation for.</typeparam>
    /// <typeparam name="TParameter1">The type to add input validation for.</typeparam>
    /// <typeparam name="TParameter2">The type to add input validation for.</typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <returns>The builder.</returns>
    public static RouteGroupBuilder WithParameterValidation<TParameter, TParameter1, TParameter2>(this RouteGroupBuilder builder) => WithParameterValidation(builder, typeof(TParameter), typeof(TParameter1), typeof(TParameter2));

    /// <summary>Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.</summary>
    /// <typeparam name="TParameter">The type to add input validation for.</typeparam>
    /// <typeparam name="TParameter1">The type to add input validation for.</typeparam>
    /// <typeparam name="TParameter2">The type to add input validation for.</typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <returns>The builder.</returns>
    public static RouteHandlerBuilder WithParameterValidation<TParameter, TParameter1, TParameter2>(this RouteHandlerBuilder builder) => WithParameterValidation(builder, typeof(TParameter), typeof(TParameter1), typeof(TParameter2));

    /// <summary>Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <param name="typeToValidate">Type to include validation for.</param>
    /// <param name="otherTypesToValidate">What other types to include validation for.</param>
    /// <returns>The builder.</returns>
    public static TBuilder WithParameterValidation<TBuilder>(this TBuilder builder, Type typeToValidate, params Type[] otherTypesToValidate) where TBuilder : IEndpointConventionBuilder {
        builder.Add(endpointBuilder => {
            var methodInfo = endpointBuilder.Metadata.OfType<MethodInfo>().FirstOrDefault();
            if (methodInfo is null) {
                return;
            }
            // Track the indicies of validatable parameters.
            List<ValidationDescriptor> parametersToValidate = null;
            foreach (var p in methodInfo.GetParameters()) {
                if (typeToValidate.Equals(p.ParameterType) || (otherTypesToValidate is not null && otherTypesToValidate.Contains(p.ParameterType))) {
                    parametersToValidate ??= new();
                    parametersToValidate.Add(new(p.Position, p.ParameterType));
                }
            }
            if (parametersToValidate is null) {
                // Nothing to validate so don't add the filter to this endpoint.
                return;
            }
            // We can respond with problem details if there's a validation error.
            endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json"));
            endpointBuilder.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async invocationContext => {
                    var validator = context.ApplicationServices.GetService<IEndpointParameterValidator>();
                    foreach (var descriptor in parametersToValidate) {
                        if (invocationContext.Arguments[descriptor.ArgumentIndex] is { } arg) {
                            var (isValid, errors) = await (validator?.TryValidateAsync(descriptor.ArgumentType, arg) ?? MiniValidator.TryValidateAsync(arg));
                            if (!isValid) {
                                return Results.ValidationProblem(errors, detail: "Model state validation", extensions: new Dictionary<string, object>() { ["code"] = "MODEL_STATE" });
                            }
                        }
                    }
                    return await next(invocationContext);
                });
            });
        });
        return builder;
    }

    private sealed record ValidationDescriptor(int ArgumentIndex, Type ArgumentType);

    /// <summary>Adds exception handling for the specified exception.</summary>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>The builder.</returns>
    public static RouteGroupBuilder WithHandledException<TException>(this RouteGroupBuilder builder, int statusCode = StatusCodes.Status400BadRequest)
        where TException : Exception => WithHandledException<RouteGroupBuilder, TException>(builder, statusCode);

    /// <summary>Adds exception handling for the specified exception.</summary>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>The builder.</returns>
    public static RouteHandlerBuilder WithHandledException<TException>(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status400BadRequest)
        where TException : Exception => WithHandledException<RouteHandlerBuilder, TException>(builder, statusCode);

    /// <summary>Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>The builder.</returns>
    public static TBuilder WithHandledException<TBuilder, TException>(this TBuilder builder, int statusCode)
        where TBuilder : IEndpointConventionBuilder
        where TException : Exception {
        builder.Add(endpointBuilder => {
            var methodInfo = endpointBuilder.Metadata.OfType<MethodInfo>().FirstOrDefault();
            if (methodInfo is null) {
                return;
            }
            // We can respond with problem details if there's a validation error.
            endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), statusCode, "application/problem+json"));
            endpointBuilder.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async invocationContext => {
                    var validator = context.ApplicationServices.GetService<IEndpointParameterValidator>();
                    try {
                        return await next(invocationContext);
                    } catch (TException ex) {
                        if (ex is BusinessException bex) {
                            return Results.ValidationProblem(bex.Errors, detail: ex.Message, extensions: new Dictionary<string, object>() { ["code"] = bex.Code });
                        } else {
                            return Results.Problem(detail: ex.Message, statusCode: statusCode, extensions: new Dictionary<string, object>() { ["code"] = typeof(TException).Name });
                        }
                    }
                });
            });
        });
        return builder;
    }
}
#endif