﻿namespace Microsoft.AspNetCore.Http;

/// <summary>Extension methods for model state validation.</summary>
public static class ValidationErrors
{
    /// <summary>Adds an error to the existing item of the validation problem dictionary.</summary>
    /// <param name="errors">Input error dictionary.</param>
    /// <param name="key">The error category key.</param>
    /// <param name="message">The error message.</param>
    public static IDictionary<string, string[]> AddError(this IDictionary<string, string[]> errors, string key, string message) {
        if (errors.TryGetValue(key, out var array)) {
            errors[key] = array.Append(message).ToArray();
        } else {
            errors.Add(key, [message]);
        }
        return errors;
    }

    /// <summary>Creates an <see cref="IDictionary{TKey, TValue}"/>and adds the first error.</summary>
    /// <param name="key">The error category key.</param>
    /// <param name="message">The error message.</param>
    public static IDictionary<string, string[]> AddError(string key, string message) {
        var errors = Create();
        return errors.AddError(key, message);
    }

    /// <summary>Creates an <see cref="IDictionary{TKey, TValue}"/>.</summary>
    public static IDictionary<string, string[]> Create() => new Dictionary<string, string[]>();

    /// <summary>
    /// Gets the first error message in the dictionary
    /// </summary>
    /// <param name="errors"></param>
    /// <returns></returns>
    public static string Detail(this IDictionary<string, string[]> errors) => errors.FirstOrDefault().Value?.FirstOrDefault();
    /// <summary>
    /// Gets the first error code
    /// </summary>
    /// <param name="errors"></param>
    /// <returns></returns>
    public static string Code(this IDictionary<string, string[]> errors) => errors.FirstOrDefault().Key;
}
