using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Indice.Types
{
    /// <summary>
    /// Α Collection wrapper that encapsulates the results of an api call or operation.
    /// Used usualy for paginated results.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResultSet<T>
    {
        /// <summary>
        /// constructs the <see cref="ResultSet{T}"/>
        /// </summary>
        public ResultSet() { }

        /// <summary>
        /// constructs the <see cref="ResultSet{T}"/>
        /// </summary>
        /// <param name="collection">Source collection. This could be a subset of the data that consists a page of the total results</param>
        /// <param name="totalCount">The total results count</param>
        public ResultSet(IEnumerable<T> collection, int totalCount) {
            Items = (collection ?? new T[0]).ToArray();
            Count = totalCount;
        }

        /// <summary>
        /// Total results count
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int Count { get; set; }

        /// <summary>
        /// The actual items collection. These could be less in number than the <see cref="Count"/> if the results referes to a page.
        /// </summary>
        public T[] Items { get; set; }
    }

    /// <summary>
    /// A subclass of <see cref="ResultSet{T}"/> that provides additional information regarding the summary row for the data
    /// </summary>
    /// <typeparam name="T">The collection item</typeparam>
    /// <typeparam name="TSummary">The summary row data type</typeparam>
    public class ResultSet<T, TSummary> : ResultSet<T>
    {
        /// <summary>
        /// constructor
        /// </summary>
        public ResultSet() : base() { }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="collection">The source collection or fragment/page of that.</param>
        /// <param name="totalCount">The total results count</param>
        /// <param name="summary">The summary row</param>
        public ResultSet(IEnumerable<T> collection, int totalCount, TSummary summary) : base(collection, totalCount) => Summary = summary;

        /// <summary>
        /// Summary row
        /// </summary>
        public TSummary Summary { get; set; }
    }
}
