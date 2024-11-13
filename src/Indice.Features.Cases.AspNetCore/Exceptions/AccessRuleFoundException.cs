namespace Indice.Features.Cases.Exceptions;

/// <summary>Exception type for invalid cases.</summary>
public class AccessRuleFoundException : Exception
{
    /// <inheritdoc />
    public AccessRuleFoundException() { }

    /// <inheritdoc />
    public AccessRuleFoundException(string message)
        : base(message) { }

    /// <inheritdoc />
    public AccessRuleFoundException(string message, Exception innerException)
        : base(message, innerException) { }

}