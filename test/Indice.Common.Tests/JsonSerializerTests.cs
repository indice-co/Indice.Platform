using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Serialization;
using Indice.Types;
using Xunit;

namespace Indice.Common.Tests
{
    public class JsonSerializerTests
    {
        [Fact]
        public void RoundtripStringToObjectTest() {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.IgnoreNullValues = true;
            var model = new { Field = "text field here", Value = 12.34 };
            var expectedJson = JsonSerializer.Serialize(model, options);
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
            options.Converters.Add(new TypeConverterJsonAdapterFactory());
            options.Converters.Add(new JsonObjectToInferredTypeConverter());
            options.IgnoreNullValues = true;
            var model = new TestTypeConverters {
                Point = GeoPoint.Parse("37.9888529,23.7037796"),
                Id = new Base64Id(Guid.Parse("04F39650-F015-4831-8592-D2D82E70589F")),
                Mystery = new TheMystery {
                    FirstName = "Constantinos",
                    LastName = "Leftheris"
                },
                Filters = new[] {
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

        [Fact]
        public void RoundtripTypeConverterAdapter_WithCollections2() {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new TypeConverterJsonAdapterFactory());
            options.IgnoreNullValues = true;
            var model = new TestModel {
                Point = GeoPoint.Parse("37.9888529,23.7037796"),
                PointList = new List<GeoPoint> {
                    GeoPoint.Parse("37.9888529,23.7037796"),
                    GeoPoint.Parse("37.9689383,23.7309977")
                }
            };
            var jsonExpected = "{\"point\":\"37.9888529,23.7037796\",\"pointList\":[\"37.9888529,23.7037796\",\"37.9689383,23.7309977\"]}";
            var json = JsonSerializer.Serialize(model, options);
            Assert.Equal(jsonExpected, json);
            var output = JsonSerializer.Deserialize<TestModel>(json, options);
            Assert.Equal(model.Point.Latitude, output.Point.Latitude);
        }

        public class TestTypeConverters
        {
            public GeoPoint Point { get; set; }
            public Base64Id Id { get; set; }
            public FilterClause[] Filters { get; set; }
            public object Mystery { get; set; }
        }

        public class TestModel
        {
            public GeoPoint Point { get; set; }
            public List<GeoPoint> PointList { get; set; }
        }

        public class TheMystery
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}
