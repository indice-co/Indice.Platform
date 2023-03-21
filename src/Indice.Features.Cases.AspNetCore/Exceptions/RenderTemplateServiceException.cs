namespace Indice.Features.Cases.Exceptions;

/// <summary>Exception type for invalid template rendering.</summary>
public class RenderTemplateServiceException : Exception
{
    /// <inheritdoc />
    public RenderTemplateServiceException() { }

    /// <inheritdoc />
    public RenderTemplateServiceException(string message)
        : base(message) { }

    /// <inheritdoc />
    public RenderTemplateServiceException(string message, Exception innerException)
        : base(message, innerException) { }
}