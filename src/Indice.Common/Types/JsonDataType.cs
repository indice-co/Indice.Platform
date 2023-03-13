namespace Indice.Types;

/// <summary>The data type representation. This is used by <see cref="FilterClause"/> &amp; <seealso cref="SortByClause"/> to identify the data type.</summary>
public enum JsonDataType : short
{
    /// <summary>String.</summary>
    String = 0,
    /// <summary>Integer.</summary>
    Integer,
    /// <summary>Number.</summary>
    Number,
    /// <summary>Boolean.</summary>
    Boolean,
    /// <summary>DateTime.</summary>
    DateTime
}
