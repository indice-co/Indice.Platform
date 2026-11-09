using System.Collections;

namespace Indice.Types;

/// <summary>A type that represents a connection string.</summary>
public sealed class ConnectionString : IEnumerable<KeyValuePair<string, string?>>
{
    private readonly IDictionary<string, string?> _properties;

    /// <summary>Creates a new instance of the <see cref="ConnectionString"/> class, using ';' as property delimiter.</summary>
    /// <param name="connectionString">The connection string.</param>
    public ConnectionString(string connectionString) : this(connectionString, ';') { }

    /// <summary>Creates a new instance of the <see cref="ConnectionString"/> class.</summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="delimiter">The character used to separate connection string properties.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="connectionString"/> is null</exception>
    /// <exception cref="ArgumentException">When <paramref name="connectionString"/> is empty</exception>
    public ConnectionString(string connectionString, char delimiter) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        Delimiter = delimiter;
        _properties = connectionString
            .Split(delimiter)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(keySelector: pair => pair[0], elementSelector: pair => pair.Length < 2 ? default : pair[1]);
    }

    /// <summary>
    /// Initializes a new instance of the ConnectionString class by copying the properties and delimiter from an
    /// existing instance.  
    /// </summary>
    /// <param name="connectionString">The ConnectionString instance to copy. Cannot be null.</param>
    public ConnectionString(ConnectionString connectionString) {
        ArgumentNullException.ThrowIfNull(connectionString);
        _properties = new Dictionary<string, string?>(connectionString._properties);
        Delimiter = connectionString.Delimiter;
    }

    /// <summary>The character used to separate connection string properties.</summary>
    public char Delimiter { get; }

    /// <summary>Gets the property associated with the specified key.</summary>
    /// <param name="key">The key whose value to get.</param>
    /// <exception cref="KeyNotFoundException"></exception>
    public string? this[string key] => _properties[key];

    /// <summary>Determines whether the connection string contains a property with the specified key.</summary>
    /// <param name="key">The key whose value to get.</param>
    public bool ContainsKey(string key) => _properties.ContainsKey(key);

    /// <summary>Gets the property associated with the specified key.</summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise null. This parameter is passed uninitialized.</param>
    public bool TryGetValue(string key, out string? value) {
        var exists = _properties.TryGetValue(key, out var foundValue);
        value = foundValue;
        return exists;
    }

    /// <summary>
    /// Removes the property with the specified key from the collection.
    /// </summary>
    /// <param name="key">The key of the property to remove. Cannot be null.</param>
    public void Remove(string key) => _properties.Remove(key);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => _properties.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    public override string ToString() => string.Join(Delimiter, this.Select(x => $"{x.Key}={x.Value}"));
}
