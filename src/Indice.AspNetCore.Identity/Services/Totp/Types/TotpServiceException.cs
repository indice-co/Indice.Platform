using System;
using System.Runtime.Serialization;

namespace Indice.AspNetCore.Identity
{
    /// <summary>Specific exception used to pass errors when using .</summary>
    [Serializable]
    public class TotpServiceException : Exception
    {
        /// <summary>Constructs the <see cref="TotpServiceException"/>.</summary>
        public TotpServiceException() { }

        /// <summary>Constructs the <see cref="TotpServiceException"/>.</summary>
        public TotpServiceException(string message) : base(message) { }

        /// <summary>Constructs the <see cref="TotpServiceException"/>.</summary>
        public TotpServiceException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>Constructs the <see cref="TotpServiceException"/>.</summary>
        protected TotpServiceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
