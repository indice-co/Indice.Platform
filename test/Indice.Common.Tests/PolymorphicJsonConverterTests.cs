using System;
using System.Collections.Generic;
using System.Text;
using Indice.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Indice.Common.Tests
{
    public class PolymorphicJsonConverterTests
    {
        public JsonSerializerSettings Settings { get; }

        public PolymorphicJsonConverterTests() {
            Settings = new JsonSerializerSettings {
                Formatting = Formatting.None,
                ContractResolver = new DefaultContractResolver {
                    NamingStrategy = new CamelCaseNamingStrategy {
                        OverrideSpecifiedNames = false
                    }
                },
                Converters = new JsonConverter[] {
                    new Newtonsoft.Json.Converters.StringEnumConverter { CamelCaseText = false },
                }
            };
        }

        [Fact]
        public void DeserializePolymorphicObjectTest() {
            var json = @"{""firstName"": ""Κωνσταντίνος"", ""lastName"": ""Λευθέρης"", ""type"": ""Student""}";

            var person = JsonConvert.DeserializeObject<Person>(json, Settings);
            Assert.IsType<Student>(person);
            Assert.Equal("Κωνσταντίνος", person.FirstName);
            Assert.Equal("Λευθέρης", person.LastName);
        }

        [Fact]
        public void DeserializePolymorphicObjectWithEnumTest() {
            var json = @"{""firstName"": ""Kate"", ""lastName"": ""Leftheris"", ""sex"": ""Female""}";

            var person = JsonConvert.DeserializeObject<Parent>(json, Settings);
            Assert.IsType<Mother>(person);
            Assert.Equal("Kate", person.FirstName);
            Assert.Equal("Leftheris", person.LastName);
        }

        [Fact]
        public void DeserializePolymorphicObjectListTest() {
            var json = @"[{""firstName"": ""Kate"", ""lastName"": ""Leftheris"", ""sex"": ""Female""}, {""firstName"": ""Κωνσταντίνος"", ""lastName"": ""Λευθέρης"", ""sex"": ""Male""}]";

            var people = JsonConvert.DeserializeObject<Parent[]>(json, Settings);
            Assert.IsType<Mother>(people[0]);
            Assert.IsType<Father>(people[1]);
        }


        [Fact]
        public void SerializePolymorphicObjectListTest() {
            var json = @"[{""type"":""Teacher"",""firstName"":""Jim"",""lastName"":""Bidis""},{""type"":""Teacher"",""firstName"":""John"",""lastName"":""Doe""},{""type"":""Student"",""firstName"":""Jane"",""lastName"":""Mary""}]";
            var people = new Person[] {
                new Teacher { FirstName = "Jim", LastName = "Bidis" },
                new Teacher { FirstName = "John", LastName = "Doe" },
                new Student { FirstName = "Jane", LastName = "Mary" },
            };
            var jsonResult = JsonConvert.SerializeObject(people, Settings);
            Assert.Equal(json, jsonResult);
        }

        [Fact]
        public void SerializePolymorphicObjectListEnumTest() {
            var json = @"[{""sex"":""Male"",""firstName"":""John"",""lastName"":""Doe""},{""sex"":""Female"",""firstName"":""Jane"",""lastName"":""Mary""}]";
            var people = new Parent[] {
                new Father { FirstName = "John", LastName = "Doe" },
                new Mother { FirstName = "Jane", LastName = "Mary" },
            };
            var jsonResult = JsonConvert.SerializeObject(people, Settings);
            Assert.Equal(json, jsonResult);
        }

        [JsonConverter(typeof(JsonPolymorphicConverter<Person>), "type")]
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

        [JsonConverter(typeof(JsonPolymorphicConverter<Parent>), "sex")]
        public abstract class Parent : Person
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
