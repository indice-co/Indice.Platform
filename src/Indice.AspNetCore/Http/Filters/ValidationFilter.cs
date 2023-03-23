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
public static class ValidationFilterExtensions
{
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
            List<int>? parameterIndexesToValidate = null;
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
            eb.Metadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), 400, "application/problem+json"));

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
    /// <param name="httpStatusCode"></param>
    /// <returns>The builder</returns>
    public static RouteGroupBuilder WithHandledException<TException>(this RouteGroupBuilder builder, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        where TException : Exception => WithHandledException<RouteGroupBuilder, TException>(builder, httpStatusCode);

    /// <summary>
    /// Adds exception handling for the specified exception.
    /// </summary>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">the builder</param>
    /// <param name="httpStatusCode"></param>
    /// <returns>The builder</returns>
    public static RouteHandlerBuilder WithHandledException<TException>(this RouteHandlerBuilder builder, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        where TException : Exception => WithHandledException<RouteHandlerBuilder, TException>(builder, httpStatusCode);


    /// <summary>
    /// Adds the validation of input parameters and <see cref="HttpValidationProblemDetails"/> automatic response when something is out of place.
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <typeparam name="TException"></typeparam>
    /// <param name="builder">the builder</param>
    /// <param name="httpStatusCode"></param>
    /// <returns>The builder</returns>
    public static TBuilder WithHandledException<TBuilder, TException>(this TBuilder builder, HttpStatusCode httpStatusCode) 
        where TBuilder : IEndpointConventionBuilder
        where TException : Exception {
        builder.Add(eb => {
            var methodInfo = eb.Metadata.OfType<MethodInfo>().FirstOrDefault();

            if (methodInfo is null) {
                return;
            }
            // We can respond with problem details if there's a validation error
            eb.Metadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), (int)httpStatusCode, "application/problem+json"));

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
                            return Results.Problem(detail: ex.Message, statusCode: (int)httpStatusCode, extensions: new Dictionary<string, object>() { ["code"] = typeof(TException).Name });
                        }
                    }
                });
            });
        });

        return builder;
    }

    // Equivalent to the .Produces call to add metadata to endpoints
    private sealed class ProducesResponseTypeMetadata : IProducesResponseTypeMetadata
    {
        public ProducesResponseTypeMetadata(Type type, int statusCode, string contentType) {
            Type = type;
            StatusCode = statusCode;
            ContentTypes = new[] { contentType };
        }

        public Type Type { get; }
        public int StatusCode { get; }
        public IEnumerable<string> ContentTypes { get; }
    }
}
#endif