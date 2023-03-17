namespace Indice.Types;

/// <summary>Extension methods over collection types that produce <see cref="ResultSet{T}"/>.</summary>
public static class ResultSetExtensions
{
    /// <summary>Creates a <see cref="ResultSet{T}"/> out of the current <seealso cref="IList{T}"/>. </summary>
    /// <typeparam name="T">The type to contain in the result items.</typeparam>
    /// <param name="collection">This is considered to be all the results. This means items length will match results count.</param>
    /// <returns>The results.</returns>
    public static ResultSet<T> ToResultSet<T>(this IList<T> collection) => new(collection, collection.Count);

    /// <summary>Creates a <see cref="ResultSet{T}"/> out of the current <seealso cref="IEnumerable{T}"/>. </summary>
    /// <typeparam name="T">The type to contain in the result items.</typeparam>
    /// <param name="collection">This is considered to be all the results. This means items length will match results count.</param>
    /// <returns>The results.</returns>
    public static ResultSet<T> ToResultSet<T>(this IEnumerable<T> collection) => new(collection, collection.Count());

    /// <summary>Creates a <see cref="ResultSet{T}"/> out of the current <seealso cref="IEnumerable{T}"/>. </summary>
    /// <typeparam name="T">The type to contain in the result items.</typeparam>
    /// <param name="collection">This is considered to be a page of the results</param>
    /// <param name="count">The total items count.</param>
    /// <returns>The results.</returns>
    public static ResultSet<T> ToResultSet<T>(this IEnumerable<T> collection, int count) => new(collection, count);

    /// <summary>Creates a <see cref="ResultSet{T}"/> out of the current queryable. This operation executes and materialises the queryable.</summary>
    /// <typeparam name="T">The type to contain in the result items.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="options">The options used for paging and sorting.</param>
    /// <returns>The results.</returns>
    public static ResultSet<T> ToResultSet<T>(this IQueryable<T> source, ListOptions options) {
        options ??= new ListOptions();
        foreach (var sorting in options.GetSortings()) {
            source = source.ApplyOrder(sorting.Path, sorting.Direction, append:true);
        }
        return source.ToResultSet(options.Page.Value, options.Size.Value);
    }

    /// <summary>Creates a <see cref="ResultSet{T}"/> out of the current queryable. This operation executes and materialises the queryable.</summary>
    /// <typeparam name="T">The type to contain in the result items.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="page">The page number (one based).</param>
    /// <param name="size">The page size.</param>
    /// <returns>The results.</returns>
    public static ResultSet<T> ToResultSet<T>(this IQueryable<T> source, int page, int size) {
        if (page <= 0) {
            throw new ArgumentOutOfRangeException(nameof(page), "Must be a positive integer");
        }
        if (size < 0) {
            throw new ArgumentOutOfRangeException(nameof(size), "Must be a positive integer");
        }
        var index = page - 1;
        if (size == 0) {
            return new ResultSet<T>(Array.Empty<T>(), source.Count());
        }
        return new ResultSet<T>(source.Skip(index * size).Take(size), source.Count());
    }
}
