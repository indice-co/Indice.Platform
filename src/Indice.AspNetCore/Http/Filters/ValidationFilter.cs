#if NET7_0_OR_GREATER
using System.Reflection;
using Indice.AspNetCore.Http.Validation;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

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
            foreach (var parameter in methodInfo.GetParameters()) {
                if (typeToValidate.Equals(parameter.ParameterType) || (otherTypesToValidate is not null && otherTypesToValidate.Contains(parameter.ParameterType))) {
                    parametersToValidate ??= [];
                    parametersToValidate.Add(new(parameter.Position, parameter.ParameterType));
                }
            }
            if (parametersToValidate is null) {
                // Nothing to validate so don't add the filter to this endpoint.
                return;
            }
            // We can respond with problem details if there's a validation error.
            endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(HttpValidationProblemDetails), [ "application/problem+json" ]));
            endpointBuilder.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async invocationContext => {
                    var validator = invocationContext.HttpContext.RequestServices.GetRequiredService<IEndpointParameterValidator>();
                    foreach (var descriptor in parametersToValidate) {
                        if (invocationContext.Arguments[descriptor.ArgumentIndex] is { } arg) {
                            var (IsValid, Errors) = await validator.TryValidateAsync(descriptor.ArgumentType, arg);
                            if (!IsValid) {
                                return Results.ValidationProblem(Errors, detail: "Model state validation", extensions: new Dictionary<string, object>() { ["code"] = "MODEL_STATE" });
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
    /// <param name="exceptionHandler">The action to perform when the exception occurs. Can be left null for default implementation.</param>
    /// <returns>The builder.</returns>
    public static RouteGroupBuilder WithHandledException<TException>(this RouteGroupBuilder builder, int statusCode = StatusCodes.Status400BadRequest, Func<TException, ValidationProblem> exceptionHandler = null)
        where TException : Exception => WithHandledException<RouteGroupBuilder, TException>(builder, statusCode, exceptionHandler);

    /// <summary>Adds exception handling for the specified exception.</summary>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="exceptionHandler">The action to perform when the exception occurs. Can be left null for default implementation.</param>
    /// <returns>The builder.</returns>
    public static RouteHandlerBuilder WithHandledException<TException>(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status400BadRequest, Func<TException, ValidationProblem> exceptionHandler = null)
        where TException : Exception => WithHandledException<RouteHandlerBuilder, TException>(builder, statusCode, exceptionHandler);

    /// <summary>Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.</summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">A builder for defining groups of endpoints with a common prefix.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="exceptionHandler">The action to perform when the exception occurs. Can be left null for default implementation.</param>
    /// <returns>The builder.</returns>
    public static TBuilder WithHandledException<TBuilder, TException>(this TBuilder builder, int statusCode, Func<TException, IResult> exceptionHandler = null)
        where TBuilder : IEndpointConventionBuilder
        where TException : Exception {
        builder.Add(endpointBuilder => {
            var methodInfo = endpointBuilder.Metadata.OfType<MethodInfo>().FirstOrDefault();
            if (methodInfo is null) {
                return;
            }
            // We can respond with problem details if there's a validation error.
            endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(statusCode, typeof(HttpValidationProblemDetails), [ "application/problem+json" ]));
            endpointBuilder.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async invocationContext => {
                    try {
                        return await next(invocationContext);
                    } catch (TException exception) {
                        if (exceptionHandler is not null) {
                            return exceptionHandler.Invoke(exception);
                        }

                        if (exception is BusinessException businessException) {
                            return Results.ValidationProblem(
                                errors: businessException.Errors,
                                detail: exception.Message,
                                extensions: new Dictionary<string, object>() { ["code"] = businessException.Code }
                            );
                        }

                        return Results.Problem(
                            detail: exception.Message,
                            statusCode: statusCode,
                            extensions: new Dictionary<string, object>() { ["code"] = typeof(TException).Name }
                        );
                    }
                });
            });
        });
        return builder;
    }
}
#endif
