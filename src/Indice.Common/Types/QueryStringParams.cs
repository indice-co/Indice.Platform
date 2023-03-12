namespace Indice.Types;

/// <summary>Custom dictionary for representing query string parameters.</summary>
public class QueryStringParams : Dictionary<string, object>
{
    /// <summary>Constructs the <see cref="QueryStringParams"/></summary>
    public QueryStringParams() { }

    /// <summary>Constructs the <see cref="QueryStringParams"/> by passing in an object that will be the source of the parameters collection.</summary>
    /// <param name="parameters"></param>
    public QueryStringParams(object parameters) => this.Merge(parameters);

    /// <summary>Converts to string.</summary>
    /// <returns></returns>
    public override string ToString() => this.ToFormUrlEncodedString();
}
