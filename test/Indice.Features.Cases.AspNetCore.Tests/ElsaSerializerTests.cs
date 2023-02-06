using Elsa.Serialization;
using Indice.Features.Cases.Converters;
using Indice.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Indice.Features.Cases.Tests;

public class ElsaSerializerTests
{
    [Fact]
    public void DecimalJsonConverterTest() {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new DecimalJsonConverter());

        var expected = "{\"Year\":2020}";
        var input = "{\"Year\":2020}";
        var obj = JObject.Parse(input);

        var output = Newtonsoft.Json.JsonConvert.SerializeObject(obj, settings);

        Assert.Equal(expected, output);

        input = "{\"Year\":2020.0}";
        obj = JObject.Parse(input);

        output = Newtonsoft.Json.JsonConvert.SerializeObject(obj, settings);
        Assert.Equal(expected, output);
    }

    [Fact]
    public void DecimalJsonConverterTest2() {
        var settings = new JsonSerializerSettings();
        DefaultContentSerializer.ConfigureDefaultJsonSerializationSettings(settings);
        settings.Converters.Add(new DecimalJsonConverter());

        var input = new TestDynamic {
            Amount = 123.12m,
            Id = 1,
            Name = "Test 1234",
            Data = JObject.Parse("{\"Year\":2020.0}")
        };

        var inputJson = Newtonsoft.Json.JsonConvert.SerializeObject(input, settings);
        var output = Newtonsoft.Json.JsonConvert.DeserializeObject<TestDynamic>(inputJson, settings);
        Assert.IsType<JObject>(output.Data);

        var systemTextSettings = new System.Text.Json.JsonSerializerOptions();
        systemTextSettings.Converters.Add(new JsonObjectToInferredTypeConverter());

        var outputJson = System.Text.Json.JsonSerializer.Serialize(output, systemTextSettings);

        Assert.True(true);
    }

    [Fact]
    public void DecimalJsonConverterTest3() {
        var settings = new JsonSerializerSettings();
        DefaultContentSerializer.ConfigureDefaultJsonSerializationSettings(settings);
        settings.Converters.Add(new DecimalJsonConverter());

        var input = new TestDynamic {
            Amount = 123.12m,
            Id = 1,
            Name = "Test 1234",
            Data = JObject.Parse("{\"Year\":2020.0}")
        };

        var systemTextSettings = new System.Text.Json.JsonSerializerOptions();
        systemTextSettings.Converters.Add(new JsonObjectToInferredTypeConverter());

        var output = System.Text.Json.JsonSerializer.Serialize(input, systemTextSettings);

        Assert.True(true);
    }
    
    public class TestDynamic
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public dynamic Data { get; set; }
    }
}