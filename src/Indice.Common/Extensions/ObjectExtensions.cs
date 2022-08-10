#if NET5_0
using System.Buffers;
#endif
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Indice.Extensions
{
    /// <summary>Extension methods on object.</summary>
    public static class ObjectExtensions
    {
        /// <summary>Converts an object to an <see cref="ExpandoObject"/>.</summary>
        /// <param name="value">The object to convert.</param>
        public static ExpandoObject ToExpandoObject(this object value) {
            if (value is null) {
                return default;
            }
            if (value is ExpandoObject expando) {
                return expando;
            }
            if (value is JsonElement json) {
#if NET5_0
                var bufferWriter = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8JsonWriter(bufferWriter)) {
                    json.WriteTo(writer);
                }
                return (ExpandoObject)JsonSerializer.Deserialize(bufferWriter.WrittenSpan, typeof(ExpandoObject));
#else 
                return json.Deserialize<ExpandoObject>();
#endif
            }

            var dataType = value.GetType();
            var obj = new ExpandoObject() as IDictionary<string, object>;
            foreach (var property in dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetIndexParameters().Length == 0)) {
                obj.Add(property.Name, property.GetValue(value, null));
            }
            return obj as ExpandoObject;
        }
    }
}
