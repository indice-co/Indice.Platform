using System;

namespace Indice.Features.Cases.Exceptions
{
    /// <summary>
    /// Exception type for invalid cases.
    /// </summary>
    public class CaseNotFoundException : Exception
    {
        public CaseNotFoundException() { }
        public CaseNotFoundException(string message)
            : base(message) { }
        public CaseNotFoundException(string message, Exception innerException)
            : base(message, innerException) { }

    }
}