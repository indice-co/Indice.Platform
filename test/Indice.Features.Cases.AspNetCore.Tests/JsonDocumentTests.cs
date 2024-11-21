#nullable enable

using System.Text.Json.Nodes;
using Indice.Features.Cases.Extensions;

namespace Indice.Features.Cases.Tests;

public class JsonDocumentTests
{
    [Fact]
    public void IndexerTest() {
        var caseData = @"
               {
                 ""name"": ""John"",
                 ""hobbies"": [
                   {
                     ""name"": ""coding""
                   },
                   {
                     ""name"": ""casing""
                   }
                 ]
              }".ToJsonNode();


        caseData["name"] = "Tom";
        caseData["surname"] = "Sawyer"; // new nodes are inserted last
        caseData["hobbies"][1]["name"] = "guitar";

        Assert.Equal(
            @"
               {
                 ""name"": ""Tom"",
                 ""hobbies"": [
                   {
                     ""name"": ""coding""
                   },
                   {
                     ""name"": ""guitar""
                   }
                 ],
                 ""surname"": ""Sawyer""
              }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void IndexerThrowsForNotExistingRootKeyTest() {
        var caseData = @"{""name"": ""John Doe""}".ToJsonNode();
        Assert.Throws<NullReferenceException>(() => caseData["father"]["occupation"] = "Psychoanalyst");
    }
    
    // ------------------------ A.1.  Adding an Object Member ------------------------//
    [Fact]
    public void JsonPatchAddObjectMemberTest() {
        var caseData = @"{""foo"": ""bar""}".ToJsonNode();

        var patch = new[] { new { op = "add", path = "/baz", value = "qux" } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{""foo"": ""bar"", ""baz"": ""qux""}".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void MergeAddObjectMemberTest() {
        var caseData = @"{""foo"": ""bar""}".ToJsonNode();
        
        caseData.Merge(new { baz = "qux" }.ToJsonNode());
        
        Assert.Equal(
            @"{""foo"": ""bar"", ""baz"": ""qux""}".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    // ------------------------ A.2.  Adding an Array Element ------------------------//
    [Fact]
    public void JsonPatchAddArrayElementTest() {
        var caseData = @"{ ""foo"": [ ""bar"", ""baz"" ] }".ToJsonNode();
        
        var patch = new[] { new { op= "add", path= "/foo/1", value= "qux" } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{ ""foo"": [ ""bar"", ""qux"", ""baz"" ] }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void MergeAddArrayElementTest() {
        var caseData = @"{ ""foo"": [ ""bar"", ""baz"" ] }".ToJsonNode();
        
        // element is added at the bottom of the array
        caseData.Merge(new { foo = new List<string> {"qux"} }.ToJsonNode()); 
        
        Assert.Equal(
            @"{ ""foo"": [ ""bar"", ""baz"", ""qux"" ] }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    
    // ------------------------ A.3.  Removing an Object Member ------------------------//
    [Fact]
    public void JsonPatchRemoveObjectMemberTest() {
        var caseData = @"{
                           ""baz"": ""qux"",
                           ""foo"": ""bar""
                         }"
            .ToJsonNode();
        
        var patch = new[] { new { op= "remove", path= "/baz" } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{ ""foo"": ""bar"" }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void MergeRemoveObjectMemberTest() {
        var caseData = @"{
                           ""baz"": ""qux"",
                           ""foo"": ""bar""
                         }"
            .ToJsonNode();
        
        // null json value is taken!
        caseData.Merge(new { baz = (string)null! }.ToJsonNode()); 
        
        Assert.Equal(
            @"{ ""foo"": ""bar"" }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    // ------------------------ A.4.  Removing an Array Element ------------------------//
    [Fact]
    public void JsonPatchRemoveArrayElementTest() {
        var caseData = @"{ ""foo"": [ ""bar"", ""qux"", ""baz"" ] }".ToJsonNode();
        
        var patch = new[] { new { op= "remove", path= "/foo/1" } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{ ""foo"": [ ""bar"", ""baz"" ] }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void MergeRemoveArrayElementTest() {
        var caseData = @"{ ""foo"": [ ""bar"", ""qux"", ""baz"" ] }".ToJsonNode();
        
        // todo: {"qux":null} is added
        caseData.Merge(new { foo = new List<object> { new { qux = (string)null! } } }.ToJsonNode());
        
        Assert.Equal(
            @"{ ""foo"": [ ""bar"", ""baz"" ] }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    // ------------------------ A.5.  Replacing a Value ------------------------//
    [Fact]
    public void JsonPatchReplaceValueTest() {
        var caseData = @"{
                           ""baz"": ""qux"",
                           ""foo"": ""bar""
                         }".ToJsonNode();
        
        var patch = new[] { new { op= "replace", path= "/baz", value = "boo" } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{
                        ""baz"": ""boo"",
                        ""foo"": ""bar""
                      }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void MergeReplaceValueTest() {
        var caseData = @"{
                           ""baz"": ""qux"",
                           ""foo"": ""bar""
                         }".ToJsonNode();
        
        caseData.Merge(new { baz = "boo" }.ToJsonNode());
        
        Assert.Equal(
            @"{
                        ""baz"": ""boo"",
                        ""foo"": ""bar""
                      }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    // ------------------------ A.6.  Moving a Value ------------------------//
    [Fact]
    public void JsonPatchMoveValueTest() {
        var caseData = @"{
                         ""foo"": {
                           ""bar"": ""baz"",
                           ""waldo"": ""fred""
                         },
                         ""qux"": {
                           ""corge"": ""grault""
                         }
        }".ToJsonNode();
        
        var patch = new[] { new { op= "move", from= "/foo/waldo", path = "/qux/thud" } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{
                        ""foo"": {
                          ""bar"": ""baz""
                        },
                        ""qux"": {
                          ""corge"": ""grault"",
                          ""thud"": ""fred""
                        }
                      }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void MergeMoveValueTest() { // todo: fix test or not support
        var caseData = @"{
                         ""foo"": {
                           ""bar"": ""baz"",
                           ""waldo"": ""fred""
                         },
                         ""qux"": {
                           ""corge"": ""grault""
                         }
        }".ToJsonNode();
        
        caseData.Merge(new { foo = new {waldo = (string) null!}, qux = new { thud = "fred" } }.ToJsonNode());
        
        Assert.Equal(
            @"{
                        ""foo"": {
                          ""bar"": ""baz""
                        },
                        ""qux"": {
                          ""corge"": ""grault"",
                          ""thud"": ""fred""
                        }
                      }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    
    // ------------------------ A.7.  Moving an Array Element ------------------------//
    [Fact]
    public void JsonPatchMoveArrayElementTest() {
        var caseData = @"{ ""foo"": [ ""all"", ""grass"", ""cows"", ""eat"" ] }".ToJsonNode();
        
        var patch = new[] { new { op= "move", from= "/foo/1", path = "/foo/3" } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{ ""foo"": [ ""all"", ""cows"", ""eat"", ""grass"" ] }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void MergeMoveArrayElementTest() { 
        var caseData = @"{ ""foo"": [ ""all"", ""grass"", ""cows"", ""eat"" ] }".ToJsonNode();

        // todo: operation not permitted
        // caseData.Merge(new { foo = new Dictionary<int, string>{{ 1, "test"}} });
        
        Assert.Equal(
            @"{ ""foo"": [ ""all"", ""cows"", ""eat"", ""grass"" ] }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    
    // ------------------------ A.10.  Adding a Nested Member Object ------------------------//
    [Fact]
    public void JsonPatchAddNestedObjectMemberTest() {
        var caseData = @"{ ""foo"": ""bar"" }".ToJsonNode();
        
        var patch = new[] { new { op= "add", path= "/child", value = new { grandchild = new {}} } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{
                         ""foo"": ""bar"",
                         ""child"": {
                           ""grandchild"": {
                           }
                         }
                       }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void MergeAddNestedObjectMemberTest() {
        var caseData = @"{ ""foo"": ""bar"" }".ToJsonNode();

        caseData.Merge(new { child = new { grandchild = new {}} }.ToJsonNode());
        
        Assert.Equal(
            @"{
                         ""foo"": ""bar"",
                         ""child"": {
                           ""grandchild"": {
                           }
                         }
                       }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    
    // ------------------------ A.10.  Adding a Nested Member Object ------------------------//
    // ----------------------- This is not supported by the Merge API -------------------------//
    
    [Fact]
    public void JsonPatchIgnoreUnrecognisedElementsTest() {
        var caseData = @"{ ""foo"": ""bar"" }".ToJsonNode();
        
        var patch = new[] { new { op= "add", path= "/baz", value = "qux", xyz = 123 } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{
                        ""foo"": ""bar"",
                        ""baz"": ""qux""
                      }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    // ------------------------ A.10.  Adding a Nested Member Object ------------------------//
    [Fact]
    public void JsonPatchAddingANonExistentTargetTest() {
        var caseData = @"{ ""foo"": ""bar"" }".ToJsonNode();
        
        var patch = new[] { new { op= "add", path= "/baz/bat", value = "qux" } }.ToJsonNode();
        Assert.Throws<InvalidOperationException>(() => caseData.ApplyJsonPatch(patch.AsArray()));
    }
    
    [Fact]
    public void MergeAddingANonExistentTargetTest() {
        var caseData = @"{ ""foo"": ""bar"" }".ToJsonNode();

        caseData.Merge(new { baz = new { bat = "qux"}  }.ToJsonNode());
        
        Assert.Equal(
            @"{
                         ""foo"": ""bar"",
                         ""baz"": {
                           ""bat"": ""qux""
                         }
                       }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    
    // ------------------------ A.14.  ~ Escape Ordering ------------------------//
    // ---------------- non c# variables for keys are not supported ------------------------//
    [Fact]
    public void JsonPatchEscapeOrderingTest() {
        var caseData = @"{
                           ""/"": 9,
                           ""~1"": 10
                         }".ToJsonNode();
        
        var patch = new[] { new { op= "add", path= "/~01", value = 20 } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{
                        ""/"": 9,
                        ""~1"": 20
                      }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    // ------------------------ A.16.  Adding an Array Value ------------------------//
    [Fact]
    public void JsonPatchAddArrayValueTest() {
        var caseData = @"{ ""foo"": [""bar""] }".ToJsonNode();
        
        var patch = new[] { new { op= "add", path= "/foo/-", value = new List<string> { "abc", "def"} } }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(
            @"{ ""foo"": [""bar"", [""abc"", ""def""]] }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void MergeAddArrayValueTest() {
        var caseData = @"{ ""foo"": [""bar""] }".ToJsonNode();

        caseData.Merge(new { foo =  new List<object> { new List<string> { "abc", "def"}} }.ToJsonNode());
        
        Assert.Equal(
            @"{ ""foo"": [""bar"", [""abc"", ""def""]] }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    

    [Fact]
    public void MergeAddTest() {
        var caseData = @"{""name"": ""John""}".ToJsonNode();
        
        caseData.Merge(new { surname = "Doe" }.ToJsonNode());
        caseData.Merge(new { nested = new { id = 22}}.ToJsonNode());

        Assert.Equal(
            @"{""name"": ""John"", ""surname"": ""Doe"", ""nested"": { ""id"": 22 }}".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void JsonPatchAddTest() {
      var caseData = @"{""name"": ""John""}".ToJsonNode();

      var patch = new List<object> {
          new { op = "add", path = "/surname", value = "Doe" },
          new { op = "add", path = "/nested", value = new { id = 22 } }
      }.ToJsonNode();
      caseData = caseData.ApplyJsonPatch(patch.AsArray());

      Assert.Equal(
        @"{""name"": ""John"", ""surname"": ""Doe"", ""nested"": { ""id"": 22 }}".ThroughConverter(),
        caseData.FromJsonNode());
    }

    [Fact]
    public void MergeNoOpTest() {
        var caseData = TestData().ToJsonNode();

        caseData.Merge(new { }.ToJsonNode());
    
        Assert.Equal(TestData().ThroughConverter(), caseData.FromJsonNode());
    }
    
    [Fact]
    public void JsonPatchNoOpTest() {
        var caseData = TestData().ToJsonNode();

        caseData.Merge(new { }.ToJsonNode());
    
        Assert.Equal(TestData().ThroughConverter(), caseData.FromJsonNode());
    }

    [Fact]
    public void MergeObjectInitializerTest() {
        var caseData = @"{""name"": ""John Doe""}".ToJsonNode();

        caseData.Merge(new { employee = new { Id = 22, Name = "Takis" } }.ToJsonNode());
        caseData.Merge(new { coordinates = new { Cartesian = new { x = 43, y = 5.05 } }}.ToJsonNode());

        Assert.Equal(
            @"{
            ""name"": ""John Doe"",
            ""employee"": {
              ""Id"": 22,
              ""Name"": ""Takis"" 
            },
            ""coordinates"": {
              ""Cartesian"": {
                ""x"": 43,
                ""y"": 5.05
              } 
           } 
        }".ThroughConverter(),
            caseData.FromJsonNode());
    }

    [Fact]
    public void StringNullWhitespaceEmptyTest() {
        var caseData = @"{""name"": ""John"", ""surname"": ""Doe"", ""middle"": ""M.""}".ToJsonNode();
        caseData.Merge(JsonNode.Parse(@"{""name"": null}")); // remove
        caseData.Merge(new { surname = string.Empty }.ToJsonNode());
        caseData.Merge(new { middle = ' ' }.ToJsonNode());
        caseData.Merge(JsonNode.Parse(@"{""father"": null}")); // remove
        caseData.Merge(new { mother = string.Empty }.ToJsonNode());
        caseData.Merge(new { grandpapa = ' ' }.ToJsonNode());
    
        Assert.Equal(
            @"{
               ""surname"": """",
               ""middle"": "" "",
               ""mother"": """",
               ""grandpapa"": "" ""
            }".ThroughConverter(),
            caseData.FromJsonNode());
    }

    [Fact]
    public void JsonTypesAreRespectedWhenMutatingTest() {
        var caseData = @"{""name"": ""John Doe""}".ToJsonNode();
        caseData.Merge(new { trueValue = true }.ToJsonNode());
        caseData.Merge(new { falseValue = false }.ToJsonNode());
        caseData.Merge(new { nullValue = (string)null! }.ToJsonNode()); // remove
        caseData.Merge(new { emptyValue = string.Empty }.ToJsonNode());
        caseData.Merge(new { intValue = 3 }.ToJsonNode());
        caseData.Merge(new { decimalValue = 2.1m }.ToJsonNode());
        caseData.Merge(new { doubleValue = 1.3 }.ToJsonNode());
        caseData.Merge(new { arrayValue = new List<int> { 1, 2 } }.ToJsonNode());
        caseData.Merge(new {
            arrayOfObjects = new List<object> { new { Object = "test" }, new { Object = "test2" } }
        }.ToJsonNode());


        Assert.Equal(
            """
                {
                   "name": "John Doe",
                   "trueValue": true,
                   "falseValue": false,
                   "emptyValue": "",
                   "intValue": 3,
                   "decimalValue": 2.1,
                   "doubleValue": 1.3,
                   "arrayValue": [1, 2],
                   "arrayOfObjects": [
                     {
                       "Object": "test"
                     },
                     {
                       "Object": "test2"
                     }
                   ]
                }
                """.ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void NestedPropertiesWithMergeTest() {
        var caseData = @"
            {
                 ""Name"": ""John Doe"",
                 ""Hobbies"": [
                   {
                     ""Books"": { 
                       ""ArtOfWar"": ""1""
                     }
                   }
                 ],
                 ""Address"": {
                   ""Location"": {
                     ""Name"": ""Somewhere"",
                     ""Cities"": [""Athens"", ""Levadia""]
                   }
                 }
            }".ToJsonNode();

        caseData.Merge(new { Name = "John Rambo" }.ToJsonNode());
        caseData.Merge(new { Hobbies = new List<object> { new { Books = new { LenaManta = "2" } } } }.ToJsonNode());
        caseData.Merge(new { Address = new { Location = new { Name = "TestLocation" } } }.ToJsonNode());
        caseData.Merge(new { Address = new { Location = new { Id = 2 } } }.ToJsonNode());
        caseData.Merge(new { Id = 5 }.ToJsonNode());
        caseData.Merge(new { Address = new { Location = new { Cities = new List<string> { "Orxomenos" } } } }.ToJsonNode());
        caseData = caseData.Merge(new { Address = new { Location = 
                new { Cities = new List<object> { new { Name = "Neverland", Coordinates = "38° 26′ 0″ N, 22° 52′ 0″ E" } } }
            }
        }.ToJsonNode());

        // todo:
        // "Hobbies": [{"Id":  2}]
        // dataAs JsonObject
        Assert.Equal(@"
                {
                  ""Name"": ""John Rambo"",
                  ""Hobbies"": [
                      {
                          ""Id"":  2
                      },
                      {
                        ""Id"": 4
                   }
                  ],
                  ""Address"": {
                      ""Location"": {
                          ""Name"": ""TestLocation"",
                          ""Cities"": [
                              ""Athens"",
                              ""Levadia"",
                              ""Orxomenos"",
                              {
                                ""Name"": ""Neverland"",
                                ""Coordinates"": ""38° 26′ 0″ N, 22° 52′ 0″ E""
                              }
                          ],
                          ""Id"": 2
                      }
                  },
                  ""Id"": 5
              }".ThroughConverter(), caseData.FromJsonNode());
    }
    
    [Fact]
    public void NestedPropertiesWithJsonPatchTest() {
        var caseData = @"
            {
                 ""Name"": ""John Doe"",
                 ""Hobbies"": [
                   {
                     ""Books"": { 
                       ""ArtOfWar"": ""1""
                     }
                   }
                 ],
                 ""Address"": {
                   ""Location"": {
                     ""Name"": ""Somewhere"",
                     ""Cities"": [""Athens"", ""Levadia""]
                   }
                 }
            }".ToJsonNode();
        
        var patch = new List<object> {
            new { op= "add", path="/Name", value = "John Rambo" },
            new { op = "add", path="/Hobbies/-", value =  new { Books = new { LenaManta = "2" } } },
            new { op = "add", path = "/Address/Location/Name", value = "TestLocation" },
            new { op = "add", path = "/Address/Location/Id", value =  2 },
            new { op = "add", path = "/Id", value = 5 },
            new { op = "add", path = "/Address/Location/Cities/-", value = "Orxomenos" },
            new { op = "add", path = "/Address/Location/Cities/-", 
                value = new { Name = "Neverland", Coordinates = "38° 26′ 0″ N, 22° 52′ 0″ E" }
            }
        }.ToJsonNode();
        
        caseData = caseData.ApplyJsonPatch(patch.AsArray());

        Assert.Equal(@"
                {
                  ""Name"": ""John Rambo"",
                  ""Hobbies"": [
                      {
                          ""Books"": {
                              ""ArtOfWar"": ""1""
                          }
                      },
                      {
                        ""Books"": {
                          ""LenaManta"": ""2""
                        }
                      }
                  ],
                  ""Address"": {
                      ""Location"": {
                          ""Name"": ""TestLocation"",
                          ""Cities"": [
                              ""Athens"",
                              ""Levadia"",
                              ""Orxomenos"",
                              {
                                ""Name"": ""Neverland"",
                                ""Coordinates"": ""38° 26′ 0″ N, 22° 52′ 0″ E""
                              }
                          ],
                          ""Id"": 2
                      }
                  },
                  ""Id"": 5
              }".ThroughConverter(), caseData.FromJsonNode());
    }

    [Fact]
    public void NestedArrayTest() {
        var caseData = @"
            {
                 ""hobbies"": [
                   {
                     ""name"": ""coding""
                   }
                 ],
                 ""martialArts"": [
                   {
                     ""name"": ""jiu jitsu""
                   }
                 ],
                 ""cities"": [
                   {
                     ""name"": ""Levadia""
                   }
                 ]
            }".ToJsonNode();

        // add an array item, cannot replace existing nested item
        caseData.Merge(new { martialArts = new List<object> { new { name = "karate" } } }.ToJsonNode());
        // remove list item
        caseData.Merge(new { cities = (string)null! }.ToJsonNode());
    
        Assert.Equal(
            @"{
               ""hobbies"": [
                   {
                     ""name"": ""coding""
                   }
                 ],
                ""martialArts"": [
                 {
                    ""name"": ""jiu jitsu""
                  },
                  {
                    ""name"": ""karate""
                  }
                ]
            }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void NestedArrayJsonPatchTest() {
        var caseData = @"
            {
                 ""hobbies"": [
                   {
                     ""name"": ""coding""
                   }
                 ],
                 ""martialArts"": [
                   {
                     ""name"": ""jiu jitsu""
                   }
                 ],
                 ""cities"": [
                   {
                     ""name"": ""Levadia""
                   }
                 ]
            }".ToJsonNode();

        var patch = new List<object> {
            new { op = "add", path = "/hobbies", value = new { newName = new List<object> { new { name = "working" } } } },
            new { op = "add", path ="/martialArts/-", value = new { name = "karate"  } },
            new { op = "remove", path = "/cities"}
        }.ToJsonNode();
        caseData = caseData.ApplyJsonPatch(patch.AsArray());
    
        Assert.Equal(
            @"{
               ""hobbies"": {
                 ""newName"": [
                   {
                     ""name"": ""working""
                   }
                 ]
                },
                ""martialArts"": [
                 {
                    ""name"": ""jiu jitsu""
                  },
                  {
                    ""name"": ""karate""
                  }
                ]
            }".ThroughConverter(),
            caseData.FromJsonNode());
    }
    
    [Fact]
    public void NestedArrayMultipleOperationsTest() {
        var caseData = @"
               {
                 ""hobbies"": [
                   {
                     ""name"": ""coding""
                   }
                 ],
                 ""martialArts"": [
                   {
                     ""name"": ""jiu jitsu""
                   }
                 ],
                 ""cities"": [
                   {
                     ""name"": ""Levadia""
                   }
                 ]
              }".ToJsonNode();

        caseData.Merge(new {
            martialArts = new List<object> { new { name = "karate" } },
            cities = (string)null!
        }.ToJsonNode());
    
        Assert.Equal(
            @"{
               ""hobbies"": [
                   {
                     ""name"": ""coding""
                   }
                 ],
                ""martialArts"": [
                 {
                    ""name"": ""jiu jitsu""
                  },
                  {
                    ""name"": ""karate""
                  }
                ]
            }".ThroughConverter(),
            caseData.FromJsonNode());
    }

    [Fact]
    public void AllOperationsWorkInOrderTest() {
        var caseData = @"
            {
                 ""name"": ""John Doe"",
                 ""hobbies"": [
                   {
                     ""name"": ""coding""
                   },
                   {
                     ""name"": ""casing""
                   }
                 ],
                 ""address"": {
                   ""location"": {
                     ""street"": ""Analipseos 1"",
                     ""neighborhood"": ""Zagaras""
                   }
                 }
            }".ToJsonNode();
    
        // remove property
        caseData.Merge(new { name = (string)null! }.ToJsonNode());
        // add properties
        caseData.Merge(new { title = "Mister" }.ToJsonNode());
        caseData.Merge(new { wife = new { name = "elsa" } }.ToJsonNode());
        // replace nested properties
        caseData.Merge(new { address = new { location = new { street = "Analipseos 22"} } }.ToJsonNode());
        caseData.Merge(new { address = new { location = new { postalCode = 32100} } }.ToJsonNode());
        caseData.Merge(new { address = new { location = new { neighborhood =  "Lykoxwri" } } }.ToJsonNode());
        // add new objects to array
        caseData.Merge(new
            { hobbies = new List<object> { new { name = new { first = "Ping", second = "Pong" } } }}.ToJsonNode());
        caseData.Merge(new { hobbies = new List<object> { new { name = new { elsa = "working"}} } }.ToJsonNode());
    
        Assert.Equal(@"
            {
                 ""hobbies"": [
                   {
                     ""name"": ""coding""
                   },
                   {
                     ""name"": ""casing""
                   },
                   {
                     ""name"": {
                       ""first"": ""Ping"",
                       ""second"": ""Pong""
                     }
                   },
                   {
                     ""name"": {
                       ""elsa"": ""working""
                     }
                   }
                 ],
                 ""address"": {
                   ""location"": {
                     ""street"": ""Analipseos 22"",
                     ""neighborhood"": ""Lykoxwri"",
                     ""postalCode"": 32100
                   }
                 },
                 ""title"": ""Mister"",
                 ""wife"": {
                   ""name"": ""elsa""
                 }
            }".ThroughConverter(), caseData.FromJsonNode());
    }

    public string TestData() {
        return @"
            {
                 ""name"": ""John Doe"",
                 ""hobbies"": [
                   {
                     ""name"": ""coding""
                   },
                   {
                     ""name"": ""casing""
                   }
                 ],
                 ""address"": {
                   ""location"": {
                     ""street"": ""Analipseos 1"",
                     ""neighborhood"": ""Zagaras""
                   }
                 }
            }";
    }
}
#nullable disable