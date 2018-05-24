using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Indice.Types
{
    public class ResultSet<T>
    {
        public ResultSet() { }

        public ResultSet(IEnumerable<T> collection, int totalCount) {
            Items = (collection ?? new T[0]).ToArray();
            Count = totalCount;
        }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int Count { get; set; }

        public T[] Items { get; set; }
    }

    public class ResultSet<T, TSummary> : ResultSet<T>
    {
        public ResultSet() : base() { }

        public ResultSet(IEnumerable<T> collection, int totalCount, TSummary summary) : base(collection, totalCount) => Summary = summary;

        public TSummary Summary { get; set; }
    }
}
