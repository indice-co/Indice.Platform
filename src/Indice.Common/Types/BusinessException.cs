using System;
using System.Collections.Generic;
using System.Linq;

namespace Indice.Types;

/// <summary>Used to propagate business information upwards through the application layers.</summary>
public class BusinessException : Exception
{
    /// <summary>Creates a new business exception.</summary>
    public BusinessException() : base() { }

    /// <summary>Creates a new business exception with the specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public BusinessException(string message) : base(message) { }

    /// <summary>Creates a new business exception with the specified error message and a list of errors.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="originOrCode">The source of the problem.</param>
    /// <param name="errors">A list of errors.</param>
    public BusinessException(string message, string originOrCode, IEnumerable<string> errors = null) : base(message) {
        Code = originOrCode;
        if (errors != null) {
            Errors.Add(originOrCode, errors.ToArray());
        }
    }

    /// <summary>The list of errors.</summary>
    public Dictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();
    /// <summary>Error code.</summary>
    public string Code { get; set; }
}
