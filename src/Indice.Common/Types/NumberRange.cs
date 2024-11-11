#if NET7_0_OR_GREATER
using System.Numerics;

namespace Indice.Types;

/// <summary>Models a number range.</summary>
/// <typeparam name="TNumberType">The numeric data type.</typeparam>
/* https://devblogs.microsoft.com/dotnet/preview-features-in-net-6-generic-math/#generic-math */
public class NumberRange<TNumberType> where TNumberType : INumber<TNumberType>
{
    /// <summary>Lower limit of range.</summary>
    public TNumberType? LowerLimit { get; set; }
    /// <summary>Upper limit of range.</summary>
    public TNumberType? UpperLimit { get; set; }
}
#endif
