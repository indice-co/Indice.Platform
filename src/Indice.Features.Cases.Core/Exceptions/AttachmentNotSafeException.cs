namespace Indice.Features.Cases.Core.Exceptions;

/// <summary>The attachment has not been cleared for download.</summary>
public class AttachmentNotSafeException : Exception
{
    /// <summary>Creates an exception for an attachment that is not safe to download.</summary>
    public AttachmentNotSafeException() : base("The attachment is not available for download because it has not been marked safe.") { }
}
