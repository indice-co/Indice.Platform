using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// Converts an object to and from JSON.
    /// </summary>
    public class PolymorphicJsonConverter<T> : PolymorphicJsonConverter {
        /// <summary>
        /// 
        /// </summary>
        public PolymorphicJsonConverter()
            : this(FindDiscriminatorProperty<T>()) {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typePropertName"></param>
        public PolymorphicJsonConverter(string typePropertName) : base (typePropertName, GetTypeMapping<T>(typePropertName)) {
        }

    }
}
