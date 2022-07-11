using System.Dynamic;
using System.Text.Json;

namespace Indice.Extensions
{
    /// <summary>Extension methods for <see cref="ExpandoObject"/>.</summary>
    public static class ExpandoObjectExtensions
    {
        /// <summary>Converts an <see cref="ExpandoObject"/> to a concrete type.</summary>
        /// <typeparam name="T">The type to convert.</typeparam>
        /// <param name="expandoObject">The <see cref="ExpandoObject"/> instance.</param>
        public static T To<T>(this ExpandoObject expandoObject) where T : class {
            var json = JsonSerializer.Serialize(expandoObject);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
