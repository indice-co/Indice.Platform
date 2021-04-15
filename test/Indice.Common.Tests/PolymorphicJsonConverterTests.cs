using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Serialization;
using Xunit;

namespace Indice.Common.Tests
{
    public class PolymorphicJsonConverterTests
    {
        public JsonSerializerOptions Options { get; }

        public PolymorphicJsonConverterTests() {
            Options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            Options.Converters.Add(new JsonStringEnumConverter());
            // ORDER is IMPORTANT in case of multiple type hierarchies with different discriminators. Specific goes first then the generic
            Options.Converters.Add(new JsonPolymorphicConverterFactory<Parent>("sex"));
            Options.Converters.Add(new JsonPolymorphicConverterFactory<Person>("type"));
            Options.IgnoreNullValues = true;
        }

        [Fact]
        public void DeserializePolymorphicObjectTest() {
            var json = @"{""firstName"": ""Κωνσταντίνος"", ""lastName"": ""Λευθέρης"", ""type"": ""Student""}";

            var person = JsonSerializer.Deserialize<Person>(json, Options);
            Assert.IsType<Student>(person);
            Assert.Equal("Κωνσταντίνος", person.FirstName);
            Assert.Equal("Λευθέρης", person.LastName);
        }

        [Fact]
        public void DeserializePolymorphicObjectWithEnumTest() {
            var json = @"{""firstName"": ""Kate"", ""lastName"": ""Leftheris"", ""sex"": ""Female""}";

            var person = JsonSerializer.Deserialize<Parent>(json, Options);
            Assert.IsType<Mother>(person);
            Assert.Equal("Kate", person.FirstName);
            Assert.Equal("Leftheris", person.LastName);
        }

        [Fact]
        public void DeserializePolymorphicObjectListTest() {
            var json = @"[{""firstName"": ""Kate"", ""lastName"": ""Leftheris"", ""sex"": ""Female""}, {""firstName"": ""Κωνσταντίνος"", ""lastName"": ""Λευθέρης"", ""sex"": ""Male""}]";

            var people = JsonSerializer.Deserialize<Parent[]>(json, Options);
            Assert.IsType<Mother>(people[0]);
            Assert.IsType<Father>(people[1]);
        }


        [Fact]
        public void SerializePolymorphicObjectListTest() {
            var json = @"[{""firstName"":""Jim"",""lastName"":""Bidis"",""type"":""Teacher""},{""firstName"":""John"",""lastName"":""Doe"",""type"":""Teacher""},{""firstName"":""Jane"",""lastName"":""Mary"",""type"":""Student""}]";
            var people = new Person[] {
                new Teacher { FirstName = "Jim", LastName = "Bidis" },
                new Teacher { FirstName = "John", LastName = "Doe" },
                new Student { FirstName = "Jane", LastName = "Mary" },
            };
            var jsonResult = JsonSerializer.Serialize(people, Options);
            Assert.Equal(json, jsonResult);
        }

        [Fact]
        public void SerializePolymorphicObjectListEnumTest() {
            var json = @"[{""sex"":""Male"",""firstName"":""John"",""lastName"":""Doe""},{""sex"":""Female"",""firstName"":""Jane"",""lastName"":""Mary""}]";
            var people = new Parent[] {
                new Father { FirstName = "John", LastName = "Doe" },
                new Mother { FirstName = "Jane", LastName = "Mary" },
            };
            var jsonResult = JsonSerializer.Serialize(people, Options);
            Assert.Equal(json, jsonResult);
        }

        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class Teacher : Person
        {
        }

        public class Student : Person
        {
        }

        public  class Parent : Person
        {
            public SexType Sex { get; set; }
        }

        public class Father : Parent
        {
            public Father() {
                Sex = SexType.Male;
            }
        }
        public class Mother : Parent
        {
            public Mother() {
                Sex = SexType.Female;
            }
        }

        public enum SexType
        {
            Unspecified = 0,
            Male = 1,
            Female = 2
        }
    }
}
