using System;

namespace Indice.Features.Cases.Exceptions
{
    /// <summary>
    /// Exception type for invalid template rendering.
    /// </summary>
    public class RenderTemplateServiceException : Exception
    {
        public RenderTemplateServiceException() { }
        public RenderTemplateServiceException(string message)
            : base(message) { }
        public RenderTemplateServiceException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}