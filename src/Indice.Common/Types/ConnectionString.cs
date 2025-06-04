namespace Indice.Types;

/// <summary>A type that represents a connection string.</summary>
public class ConnectionString
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
        _properties = connectionString
            .Split(delimiter)
            .Select(pair => pair.Split('='))
            .ToDictionary(keySelector: pair => pair[0], elementSelector: pair => pair.Length < 2 ? default : pair[1]);
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
}
