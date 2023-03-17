namespace Indice.Features.Cases.Exceptions;

/// <summary>Exception type for invalid cases.</summary>
public class CaseNotFoundException : Exception
{
    /// <inheritdoc />
    public CaseNotFoundException() { }

    /// <inheritdoc />
    public CaseNotFoundException(string message)
        : base(message) { }

    /// <inheritdoc />
    public CaseNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }

}