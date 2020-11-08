using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Serialization;
using Indice.Types;
using Xunit;

namespace Indice.Common.Tests
{
    public class TextJsonTests
    {
        [Fact]
        public void RoundtripStringToObjectTest() {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.IgnoreNullValues = true;
            var model = new { Field = "text field here", Value = 12.34 };
            var expectedJson = JsonSerializer.Serialize(model, options) ;
            var doc = JsonDocument.Parse(expectedJson);
            var outputJson = JsonSerializer.Serialize(doc.RootElement);
            Assert.Equal(expectedJson, outputJson);
        }
        
        [Fact]
        public void RoundtripTypeConverterAdapter() {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new TypeConverterJsonAdapter());
            options.IgnoreNullValues = true;
            var model = new TestTypeConverters { Point = GeoPoint.Parse("37.9888529,23.7037796"), Id = new Base64Id(Guid.Parse("04F39650-F015-4831-8592-D2D82E70589F")) };
            var jsonExpected = "{\"point\":\"37.9888529,23.7037796\",\"id\":\"UJbzBBXwMUiFktLYLnBYnw\"}";
            var json = JsonSerializer.Serialize(model, options);
            Assert.Equal(jsonExpected, json);
            var output = JsonSerializer.Deserialize<TestTypeConverters>(json, options);
            Assert.Equal(model.Point.Latitude, output.Point.Latitude);
            Assert.Equal(model.Id, output.Id);
        }

        [Fact]
        public void RoundtripTypeConverterAdapterWithCollections() {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new TypeConverterJsonAdapter());
            options.Converters.Add(new JsonObjectToInferredTypeConverter());
            options.IgnoreNullValues = true;
            var model = new TestTypeConverters { 
                Point = GeoPoint.Parse("37.9888529,23.7037796"), 
                Id = new Base64Id(Guid.Parse("04F39650-F015-4831-8592-D2D82E70589F")), 
                Mystery = new TheMystery() { FirstName = "Constantinos", LastName = "Leftheris" },
                Filters = new [] {
                    FilterClause.Parse("name::In::(String)Constantinos, George")
                }
            };
            var jsonExpected = "{\"point\":\"37.9888529,23.7037796\",\"id\":\"UJbzBBXwMUiFktLYLnBYnw\",\"filters\":[\"name::In::(String)Constantinos, George\"],\"mystery\":{\"firstName\":\"Constantinos\",\"lastName\":\"Leftheris\"}}";
            //var jsonExpected = "{\"point\":\"37.9888529,23.7037796\",\"id\":\"UJbzBBXwMUiFktLYLnBYnw\",\"mystery\":{\"firstName\":\"Constantinos\",\"lastName\":\"Leftheris\"}}";
            var json = JsonSerializer.Serialize(model, options);
            Assert.Equal(jsonExpected, json);
            var output = JsonSerializer.Deserialize<TestTypeConverters>(json, options);
            Assert.Equal(model.Point.Latitude, output.Point.Latitude);
            Assert.Equal(model.Id, output.Id);
            var json2 = JsonSerializer.Serialize(output, options);
            Assert.Equal(json, json2);
        }

        class TestTypeConverters
        {
            public GeoPoint Point { get; set; }
            public Base64Id Id { get; set; }
            public FilterClause[] Filters { get; set; }
            public object Mystery { get; set; }
        }

        public class TheMystery
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
        
        ////https://github.com/dotnet/runtime/blob/master/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Collection/ArrayConverter.cs
        ///// <summary>
        ///// Converter for <cref>System.Array</cref>.
        ///// </summary>
        //internal sealed class ArrayConverter<TCollection, TElement>
        //: IEnumerableDefaultConverter<TCollection, TElement>
        //where TCollection : IEnumerable
        //{
        //    internal override bool CanHaveIdMetadata => false;

        //    protected override void Add(in TElement value, ref ReadStack state) {
        //        ((List<TElement>)state.Current.ReturnValue!).Add(value);
        //    }

        //    protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options) {
        //        state.Current.ReturnValue = new List<TElement>();
        //    }

        //    protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options) {
        //        List<TElement> list = (List<TElement>)state.Current.ReturnValue!;
        //        state.Current.ReturnValue = list.ToArray();
        //    }

        //    protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state) {
        //        TElement[] array = (TElement[])(IEnumerable)value;

        //        int index = state.Current.EnumeratorIndex;

        //        JsonConverter<TElement> elementConverter = GetElementConverter(ref state);
        //        if (elementConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null) {
        //            // Fast path that avoids validation and extra indirection.
        //            for (; index < array.Length; index++) {
        //                elementConverter.Write(writer, array[index], options);
        //            }
        //        } else {
        //            for (; index < array.Length; index++) {
        //                TElement element = array[index];
        //                if (!elementConverter.TryWrite(writer, element, options, ref state)) {
        //                    state.Current.EnumeratorIndex = index;
        //                    return false;
        //                }

        //                if (ShouldFlush(writer, ref state)) {
        //                    state.Current.EnumeratorIndex = ++index;
        //                    return false;
        //                }
        //            }
        //        }

        //        return true;
        //    }
        //}
    }

}
