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

        [Fact]
        public void SupportValueTuple() {
            var objectWithValueTuple = new WeatherForecastValueTuple {
                Location = (3, 7),
                Temp = new ValueTuple<string>("FOO"),
                Tuple = (
                    8,
                    new Poco {
                        Alpha = 9,
                        Beta = (new List<int>() { -1, 0, -1 }, "a list")
                    },
                    new ValueTuple<string>("Bar"))
            };

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ValueTupleJsonConverterFactory());

            string jsonString = JsonSerializer.Serialize(objectWithValueTuple, options);
            Console.WriteLine(jsonString);

            WeatherForecastValueTuple roundTrip = JsonSerializer.Deserialize<WeatherForecastValueTuple>(jsonString, options);
            Assert.Equal((3, 7), roundTrip.Location);
            Assert.Equal(new ValueTuple<string>("FOO"), roundTrip.Temp);
        }

        [Fact]
        public void DictioaryOfEnumObject_MustSerialize() {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new DictionaryEnumConverter());
            options.IgnoreNullValues = true;
            var model = new MusicLibrary { 
                Tracks = new Dictionary<MusicGenre, List<MusicTrack>> { 
                    [MusicGenre.Metal] = new List<MusicTrack> { 
                        new MusicTrack { Genre = MusicGenre.Metal, Name = "For whoom the bell tolls", Artist = "Metallica", ReleaseDate = DateTimeOffset.Parse("2021-04-15T18:36:09.2769853+03:00")},
                        new MusicTrack { Genre = MusicGenre.Metal, Name = "Tornado of souls", Artist = "Megadeth", ReleaseDate = DateTimeOffset.Parse("2021-04-15T18:36:09.2769853+03:00") },
                    },
                    [MusicGenre.Pop] = new List<MusicTrack> {
                        new MusicTrack { Genre = MusicGenre.Pop, Name = "Saturday night fever", Artist = "BGs", ReleaseDate = DateTimeOffset.Parse("2021-04-15T18:36:09.2769853+03:00") },
                    },
                    [MusicGenre.Rock] = new List<MusicTrack> {
                        new MusicTrack { Genre = MusicGenre.Rock, Name = "Rock of ages", Artist = "Def Leppard", ReleaseDate = DateTimeOffset.Parse("2021-04-15T18:36:09.2769853+03:00") },
                    }
                }
            };
            var jsonExpected = "{\"tracks\":{\"Metal\":[{\"name\":\"For whoom the bell tolls\",\"genre\":\"Metal\",\"artist\":\"Metallica\",\"releaseDate\":\"2021-04-15T18:36:09.2769853+03:00\"},{\"name\":\"Tornado of souls\",\"genre\":\"Metal\",\"artist\":\"Megadeth\",\"releaseDate\":\"2021-04-15T18:36:09.2769853+03:00\"}],\"Pop\":[{\"name\":\"Saturday night fever\",\"genre\":\"Pop\",\"artist\":\"BGs\",\"releaseDate\":\"2021-04-15T18:36:09.2769853+03:00\"}],\"Rock\":[{\"name\":\"Rock of ages\",\"genre\":\"Rock\",\"artist\":\"Def Leppard\",\"releaseDate\":\"2021-04-15T18:36:09.2769853+03:00\"}]}}";
            var json = JsonSerializer.Serialize(model, options);
            Assert.Equal(jsonExpected, json);
            var output = JsonSerializer.Deserialize<MusicLibrary>(json, options);
        }

        [Fact]
        public void Boolean_MustDeserialize_ToString() {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new JsonAnyStringConverter());
            options.IgnoreNullValues = true;
            var sourceModel = new PocoValue<bool> { Value = true };
            var json = JsonSerializer.Serialize(sourceModel, options);
            var targetModel = JsonSerializer.Deserialize<PocoValue<string>>(json, options);

            var sourceModel1 = new PocoValue<TheMystery> { Value = new TheMystery { FirstName = "Gus", LastName = "Coin" } };
            json = JsonSerializer.Serialize(sourceModel1, options);
            var targetModel1 = JsonSerializer.Deserialize<PocoValue<string>>(json, options);

            var sourceModel2 = new PocoValue<double> { Value = 1010.45 };
            json = JsonSerializer.Serialize(sourceModel2, options);
            var targetModel2 = JsonSerializer.Deserialize<PocoValue<string>>(json, options);

        }
        [Fact]
        public void TimeSpan_JsonSupport() {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.IgnoreNullValues = true;
            Assert.ThrowsAny<Exception>(() => RoundtripSerialize(new PocoValue<TimeSpan?> { Value = new TimeSpan(2, 30, 12) }, options));
            Assert.ThrowsAny<Exception>(() => RoundtripSerialize(new PocoValue<TimeSpan> { Value = new TimeSpan(2, 30, 12) }, options));
            RoundtripSerialize(new PocoValue<TimeSpan?>(), options);
            options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new JsonTimeSpanConverter());
            options.Converters.Add(new JsonNullableTimeSpanConverter());
            options.IgnoreNullValues = true;
            RoundtripSerialize(new PocoValue<TimeSpan?> { Value = new TimeSpan(2, 30, 12) }, options);
            RoundtripSerialize(new PocoValue<TimeSpan> { Value = new TimeSpan(2, 30, 12) }, options);
            RoundtripSerialize(new PocoValue<TimeSpan?>(), options);
        }

        private void RoundtripSerialize<T>(PocoValue<T> source, JsonSerializerOptions options) {
            var json = JsonSerializer.Serialize(source, options);
            var result = JsonSerializer.Deserialize<PocoValue<T>>(json, options);
            Assert.Equal(source.Value, result.Value);
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

        public class WeatherForecastValueTuple
        {
            public ValueTuple<string> Temp { get; set; }
            public (int x, int y) Location { get; set; }
            public (int a, Poco b, ValueTuple<string> c) Tuple { get; set; }
        }

        public class Poco
        {
            public int Alpha { get; set; }
            public (List<int>, string) Beta { get; set; }
        }

        public class PocoValue<T>
        {
            public T Value { get; set; }
        }

        public enum MusicGenre : int
        { 
            Metal = 1,
            Rock,
            Goth,
            Pop,
            Disco,
            RnB
        }

        public class MusicTrack
        {
            public string Name { get; set; }
            public MusicGenre Genre { get; set; }
            public string Artist { get; set; }
            public DateTimeOffset ReleaseDate { get; set; }
        }

        public class MusicLibrary 
        {
            public Dictionary<MusicGenre, List<MusicTrack>> Tracks { get; set; } = new Dictionary<MusicGenre, List<MusicTrack>>();
        }
    }
}
