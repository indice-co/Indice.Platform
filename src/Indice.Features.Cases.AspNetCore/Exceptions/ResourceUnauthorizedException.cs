using System;

namespace Indice.Features.Cases.Exceptions
{
    /// <summary>
    /// Exception type for the unauthorized access to a case.
    /// </summary>
    public class ResourceUnauthorizedException : Exception
    {
        public ResourceUnauthorizedException() { }
        public ResourceUnauthorizedException(string message)
            : base(message) { }
        public ResourceUnauthorizedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
