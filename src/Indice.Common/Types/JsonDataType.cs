namespace Indice.Types
{
    /// <summary>
    /// The data type representation. This is used by <see cref="FilterClause"/> &amp; <seealso cref="SortByClause"/> 
    /// to identify the data type.
    /// </summary>
    public enum JsonDataType : short
    {
        String = 0,
        Integer,
        Number,
        Boolean,
        DateTime,
    }
}
