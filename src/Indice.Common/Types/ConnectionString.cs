namespace Indice.Types
{
    /// <summary>A type that represents a connection string.</summary>
    public class ConnectionString
    {
        private readonly IDictionary<string, string> _properties;

        /// <summary>Creates a new instance of the <see cref="ConnectionString"/> class.</summary>
        /// <param name="connectionString">The connection string.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ConnectionString(string connectionString) {
            if (string.IsNullOrWhiteSpace(connectionString)) {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null, empty or white space.");
            }
            _properties = connectionString
                .Split(';')
                .Select(pair => pair.Split('='))
                .ToDictionary(keySelector: pair => pair[0], elementSelector: pair => pair.Length < 2 ? default : pair[1]);
        }

        /// <summary>Gets the property associated with the specified key.</summary>
        /// <param name="key">The key whose value to get.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public string this[string key] => _properties[key];

        /// <summary>Determines whether the connection string contains a property with the specified key.</summary>
        /// <param name="key">The key whose value to get.</param>
        public bool ContainsProperty(string key) => _properties.ContainsKey(key);

        /// <summary>Gets the property associated with the specified key.</summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise null. This parameter is passed uninitialized.</param>
        public bool TryGetValue(string key, out string value) {
            var exists = _properties.TryGetValue(key, out var foundValue);
            value = foundValue;
            return exists;
        }
    }
}
