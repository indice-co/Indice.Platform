using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Reflection;

namespace Indice.Serialization
{
    /// <summary>
    /// Adapter between <see cref="System.ComponentModel.TypeConverter"/> and <see cref="JsonConverter"/>
    /// </summary>
    public class TypeConverterJsonAdapter : JsonConverter<object>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override object Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) {

            var converter = TypeDescriptor.GetConverter(typeToConvert);
            var text = reader.GetString();
            return converter.ConvertFromString(text);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="objectToWrite"></param>
        /// <param name="options"></param>
        public override void Write(
            Utf8JsonWriter writer,
            object objectToWrite,
            JsonSerializerOptions options) {

            var converter = TypeDescriptor.GetConverter(objectToWrite);
            var text = converter.ConvertToString(objectToWrite);
            writer.WriteStringValue(text);
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeToConvert"></param>
        /// <returns></returns>
        public override bool CanConvert(Type typeToConvert) {
            var hasConverter = typeToConvert.GetCustomAttributes<TypeConverterAttribute>(inherit: true).Any();
            if (!hasConverter)
                return false;
            return true;
        }
    }
}
