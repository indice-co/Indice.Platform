using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;

namespace Indice.AspNetCore.Extensions
{
    /// <summary>
    /// Extensions on type <see cref="ITempDataDictionary"/>
    /// </summary>
    public static class TempDataExtensions
    {
        /// <summary>
        /// Adds information to the <see cref="TempDataDictionary"/> using a specified key.
        /// </summary>
        /// <typeparam name="T">The type of data to persist.</typeparam>
        /// <param name="tempData">Represents a set of data that persists only from one request to the next.</param>
        /// <param name="key">The key to use in the <see cref="TempDataDictionary"/>.</param>
        /// <param name="value">The value to persist.</param>
        public static void Put<T>(this ITempDataDictionary tempData, string key, T value) => tempData[key] = JsonConvert.SerializeObject(value);

        /// <summary>
        /// Retrieves information from the <see cref="TempDataDictionary"/> using a specified key.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve.</typeparam>
        /// <param name="tempData">Represents a set of data that persists only from one request to the next.</param>
        /// <param name="key">The key to use in the <see cref="TempDataDictionary"/>.</param>
        /// <returns>The data persisted in the <see cref="TempDataDictionary"/> under the specified key.</returns>
        public static T Get<T>(this ITempDataDictionary tempData, string key) {
            tempData.TryGetValue(key, out var @object);
            return @object == null ? default : JsonConvert.DeserializeObject<T>((string)@object);
        }

        /// <summary>
        /// Retrieves information from the <see cref="TempDataDictionary"/> using a specified key, without marking the key for deletion.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve.</typeparam>
        /// <param name="tempData">Represents a set of data that persists only from one request to the next.</param>
        /// <param name="key">The key to use in the <see cref="TempDataDictionary"/>.</param>
        /// <returns>The data persisted in the <see cref="TempDataDictionary"/> under the specified key.</returns>
        public static T Peek<T>(this ITempDataDictionary tempData, string key) {
            var item = tempData.Peek(key);
            if (item != null) {
                return JsonConvert.DeserializeObject<T>((string)item);
            }
            return default;
        }
    }
}
