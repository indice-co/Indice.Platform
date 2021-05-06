using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Indice.Extensions
{
    /// <summary>
    /// Extension methods related to JSON serialization.
    /// </summary>
    public static partial class JsonExtensions
    {
        /// <summary>
        /// Converts a <see cref="JsonElement"/> to the given type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null) {
            var json = element.GetRawText();
            return JsonSerializer.Deserialize<T>(json, options);
        }

        /// <summary>
        /// Converts a <see cref="JsonDocument"/> to the given type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="document"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions options = null) =>
            ToObject<T>(document.RootElement, options);

        /// <summary>
        /// Converts a <see cref="JsonElement"/> to the given type <paramref name="returnType"/>
        /// </summary>
        /// <param name="element"></param>
        /// <param name="returnType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static object ToObject(this JsonElement element, Type returnType, JsonSerializerOptions options = null) {
            var json = element.GetRawText();
            return JsonSerializer.Deserialize(json, returnType, options);
        }

        /// <summary>
        /// Converts a <see cref="JsonDocument"/> to the given type <paramref name="returnType"/>
        /// </summary>
        /// <param name="document"></param>
        /// <param name="returnType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static object ToObject(this JsonDocument document, Type returnType, JsonSerializerOptions options = null) =>
            ToObject(document.RootElement, returnType, options);
    }
}
