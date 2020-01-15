using System;
using System.Collections.Generic;
using System.Text;
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
            var options = new JsonSerializerOptions();
            // Support for OpenAPI / Swagger when using System.Text.Json is ongoing and unlikely to be available as part of the 3.0 release.
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
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
            var options = new JsonSerializerOptions();
            // Support for OpenAPI / Swagger when using System.Text.Json is ongoing and unlikely to be available as part of the 3.0 release.
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
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

        class TestTypeConverters
        {
            public GeoPoint Point { get; set; }
            public Base64Id Id { get; set; }
        }
    }
}
