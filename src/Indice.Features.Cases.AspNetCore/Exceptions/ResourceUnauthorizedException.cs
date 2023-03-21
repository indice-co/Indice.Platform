namespace Indice.Features.Cases.Exceptions;

/// <summary>Exception type for the unauthorized access to a case.</summary>
public class ResourceUnauthorizedException : Exception
{
    /// <inheritdoc />
    public ResourceUnauthorizedException() { }
    
    /// <inheritdoc />
    public ResourceUnauthorizedException(string message)
        : base(message) { }

    /// <inheritdoc />
    public ResourceUnauthorizedException(string message, Exception innerException)
        : base(message, innerException) { }
}
