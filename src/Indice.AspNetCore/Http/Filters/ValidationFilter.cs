#if NET7_0_OR_GREATER
using System.Net;
using System.Reflection;
using Indice.AspNetCore.Http.Validation;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MiniValidation;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Model state validation convention extensions
/// </summary>
public static partial class ValidationFilterExtensions
{
    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TParameter">The type to add input validation for</typeparam>
    /// <param name="builder"></param>
    /// <returns>The builder</returns>
    public static RouteGroupBuilder WithParameterValidation<TParameter>(this RouteGroupBuilder builder)
        => WithParameterValidation(builder, typeof(TParameter));
    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TParameter">The type to add input validation for</typeparam>
    /// <param name="builder"></param>
    /// <returns>The builder</returns>
    public static RouteHandlerBuilder WithParameterValidation<TParameter>(this RouteHandlerBuilder builder)
        => WithParameterValidation(builder, typeof(TParameter));
    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TParameter">The type to add input validation for</typeparam>
    /// <typeparam name="TParameter1">The type to add input validation for</typeparam>
    /// <param name="builder"></param>
    /// <returns>The builder</returns>
    public static RouteGroupBuilder WithParameterValidation<TParameter, TParameter1>(this RouteGroupBuilder builder)
         => WithParameterValidation(builder, typeof(TParameter), typeof(TParameter1));
    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TParameter">The type to add input validation for</typeparam>
    /// <typeparam name="TParameter1">The type to add input validation for</typeparam>
    /// <param name="builder"></param>
    /// <returns>The builder</returns>
    public static RouteHandlerBuilder WithParameterValidation<TParameter, TParameter1>(this RouteHandlerBuilder builder)
         => WithParameterValidation(builder, typeof(TParameter), typeof(TParameter1));
    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TParameter">The type to add input validation for</typeparam>
    /// <typeparam name="TParameter1">The type to add input validation for</typeparam>
    /// <typeparam name="TParameter2">The type to add input validation for</typeparam>
    /// <param name="builder"></param>
    /// <returns>The builder</returns>
    public static RouteGroupBuilder WithParameterValidation<TParameter, TParameter1, TParameter2>(this RouteGroupBuilder builder)
         => WithParameterValidation(builder, typeof(TParameter), typeof(TParameter1), typeof(TParameter2));
    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TParameter">The type to add input validation for</typeparam>
    /// <typeparam name="TParameter1">The type to add input validation for</typeparam>
    /// <typeparam name="TParameter2">The type to add input validation for</typeparam>
    /// <param name="builder"></param>
    /// <returns>The builder</returns>
    public static RouteHandlerBuilder WithParameterValidation<TParameter, TParameter1, TParameter2>(this RouteHandlerBuilder builder)
         => WithParameterValidation(builder, typeof(TParameter), typeof(TParameter1), typeof(TParameter2));


    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">the builder</param>
    /// <param name="typeToValidate">Type to include validation for</param>
    /// <param name="otherTypesToValidate">What other types to include validation for</param>
    /// <returns>The builder</returns>
    public static TBuilder WithParameterValidation<TBuilder>(this TBuilder builder, Type typeToValidate, params Type[] otherTypesToValidate) where TBuilder : IEndpointConventionBuilder {
        builder.Add(eb => {
            var methodInfo = eb.Metadata.OfType<MethodInfo>().FirstOrDefault();

            if (methodInfo is null) {
                return;
            }

            // Track the indicies of validatable parameters
            List<int> parameterIndexesToValidate = null;
            foreach (var p in methodInfo.GetParameters()) {
                if (typeToValidate.Equals(p.ParameterType) || otherTypesToValidate.Contains(p.ParameterType)) {
                    parameterIndexesToValidate ??= new();
                    parameterIndexesToValidate.Add(p.Position);
                }
            }

            if (parameterIndexesToValidate is null) {
                // Nothing to validate so don't add the filter to this endpoint
                return;
            }

            // We can respond with problem details if there's a validation error
            eb.Metadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json"));

            eb.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async (efic) => {
                    var validator = context.ApplicationServices.GetService<IEndpointParameterValidator>();
                    foreach (var index in parameterIndexesToValidate) {
                        if (efic.Arguments[index] is { } arg) {
                            var (isValid, errors) = await (validator?.TryValidateAsync(arg) ?? MiniValidator.TryValidateAsync(arg));
                            if (!isValid) {
                                return Results.ValidationProblem(errors, detail: "Model state validation", extensions: new Dictionary<string, object>() { ["code"] = "MODEL_STATE" });
                            }
                        }
                    }
                    return await next(efic);
                });
            });
        });

        return builder;
    }

    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">the builder</param>
    /// <param name="typesToValidate">What types to include validation for</param>
    /// <returns>The builder</returns>
    public static TBuilder WithParameterValidation<TBuilder>(this TBuilder builder, params Type[] typesToValidate) where TBuilder : IEndpointConventionBuilder {
        builder.Add(eb => {
            var methodInfo = eb.Metadata.OfType<MethodInfo>().FirstOrDefault();

            if (methodInfo is null) {
                return;
            }

            // Track the indicies of validatable parameters
            List<int> parameterIndexesToValidate = null;
            foreach (var p in methodInfo.GetParameters()) {
                if (typesToValidate.Contains(p.ParameterType)) {
                    parameterIndexesToValidate ??= new();
                    parameterIndexesToValidate.Add(p.Position);
                }
            }

            if (parameterIndexesToValidate is null) {
                // Nothing to validate so don't add the filter to this endpoint
                return;
            }

            // We can respond with problem details if there's a validation error
            eb.Metadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json"));

            eb.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async (efic) => {
                    var validator = context.ApplicationServices.GetService<IEndpointParameterValidator>();
                    foreach (var index in parameterIndexesToValidate) {
                        if (efic.Arguments[index] is { } arg) {
                            var (isValid, errors) = await (validator?.TryValidateAsync(arg) ?? MiniValidator.TryValidateAsync(arg));
                            if (!isValid) { 
                                return Results.ValidationProblem(errors, detail:"Model state validation", extensions: new Dictionary<string, object>() { ["code"] = "MODEL_STATE" });
                            }
                        }
                    }
                    return await next(efic);
                });
            });
        });

        return builder;
    }

    /// <summary>
    /// Adds exception handling for the specified exception.
    /// </summary>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">the builder</param>
    /// <param name="statusCode"></param>
    /// <returns>The builder</returns>
    public static RouteGroupBuilder WithHandledException<TException>(this RouteGroupBuilder builder, int statusCode = StatusCodes.Status400BadRequest)
        where TException : Exception => WithHandledException<RouteGroupBuilder, TException>(builder, statusCode); 

    /// <summary>
    /// Adds exception handling for the specified exception.
    /// </summary>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">the builder</param>
    /// <param name="statusCode"></param>
    /// <returns>The builder</returns>
    public static RouteHandlerBuilder WithHandledException<TException>(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status400BadRequest)
        where TException : Exception => WithHandledException<RouteHandlerBuilder, TException>(builder, statusCode);


    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">the builder</param>
    /// <param name="statusCode"></param>
    /// <returns>The builder</returns>
    public static TBuilder WithHandledException<TBuilder, TException>(this TBuilder builder, int statusCode) 
        where TBuilder : IEndpointConventionBuilder
        where TException : Exception {
        builder.Add(eb => {
            var methodInfo = eb.Metadata.OfType<MethodInfo>().FirstOrDefault();

            if (methodInfo is null) {
                return;
            }
            // We can respond with problem details if there's a validation error
            eb.Metadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), statusCode, "application/problem+json"));

            eb.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async (efic) => {
                    var validator = context.ApplicationServices.GetService<IEndpointParameterValidator>();
                    try {
                        return await next(efic);
                    } 
                    catch (TException ex) {
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