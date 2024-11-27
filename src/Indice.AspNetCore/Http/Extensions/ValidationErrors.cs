using System.Collections;

namespace Microsoft.AspNetCore.Http;

/// <summary>Extension methods for model state validation.</summary>
public static class ValidationErrors
{
    /// <summary>Adds an error to the existing item of the validation problem dictionary.</summary>
    /// <param name="errors">Input error dictionary.</param>
    /// <param name="key">The error category key.</param>
    /// <param name="message">The error message.</param>
    public static IDictionary<string, string[]> AddError(this IDictionary<string, string[]> errors, string key, string message) {
        if (errors.TryGetValue(key, out var array)) {
            errors[key] = [.. array, message];
        } else {
            errors.Add(key, [message]);
        }
        return errors;
    }
    
    /// <summary>Adds an error to the existing item of the validation problem dictionary.</summary>
    /// <param name="errors">Input error dictionary.</param>
    /// <param name="key">The error category key.</param>
    /// <param name="messages">A range of error messages.</param>
    public static IDictionary<string, string[]> AddErrors(this IDictionary<string, string[]> errors, string key, IEnumerable<string> messages) {
        if (errors.TryGetValue(key, out var array)) {
            errors[key] = [.. array, .. messages];
        } else {
            errors.Add(key, [.. messages]);
        }
        return errors;
    }

    /// <summary>Creates an <see cref="IDictionary{TKey, TValue}"/>and adds the first error.</summary>
    /// <param name="key">The error category key.</param>
    /// <param name="message">The error message.</param>
    public static IDictionary<string, string[]> AddError(string key, string message) => Create().AddError(key, message);


    /// <summary>Creates an <see cref="IDictionary{TKey, TValue}"/>and adds the first error range.</summary>
    /// <param name="key">The error category key.</param>
    /// <param name="messages">A range of error messages.</param>
    public static IDictionary<string, string[]> AddErrors(string key, IEnumerable<string> messages) => Create().AddErrors(key, messages);

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
