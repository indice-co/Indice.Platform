using System.Collections.Generic;

namespace Indice.AspNetCore.Types
{
    /// <summary>
    /// Custom dictionary for representing querystring parameters.
    /// </summary>
    public class QueryStringParams : Dictionary<string, object>
    {
        public QueryStringParams() { }

        public QueryStringParams(object parameters) => this.Merge(parameters);

        public override string ToString() => this.ToFormUrlEncodedString();
    }
}
