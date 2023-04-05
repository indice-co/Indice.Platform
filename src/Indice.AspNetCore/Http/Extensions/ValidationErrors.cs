namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods for model state validation.
/// </summary>
public static class ValidationErrors
{
    /// <summary>
    /// Adds an error to the existing item of the validation problem dictionary.
    /// </summary>
    /// <param name="errors">Input error dictionary</param>
    /// <param name="key">The error category key</param>
    /// <param name="message">The error message</param>
    /// <returns></returns>
    public static IDictionary<string, string[]> AddError(this IDictionary<string, string[]> errors, string key, string message) {
        if (errors.TryGetValue(key, out var array)) {
            errors[key] = array.Append(message).ToArray();
        } else {
            errors.Add(key, new[] { message });
        }
        return errors;
    }

    /// <summary>
    /// Creates an <see cref="IDictionary{TKey, TValue}"/>and adds the first error.
    /// </summary>
    /// <param name="key">The error category key</param>
    /// <param name="message">The error message</param>
    /// <returns></returns>
    public static IDictionary<string, string[]> AddError(string key, string message) {
        var errors = Create();
        return errors.AddError(key, message);
    }

    /// <summary>
    /// Creates an <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <returns></returns>
    public static IDictionary<string, string[]> Create() => new Dictionary<string, string[]>();
}
