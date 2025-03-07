#nullable enable

using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models;
using Json.Patch;

namespace Indice.Features.Cases.Tests;

/// <summary>JsonNodeMergeTests</summary>
/// <remarks>Changes should be kept up to date with
/// https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API
/// </remarks>
public class JsonNodeMergeTests
{
    // ------------------------ A.1.  Adding an Object Member ------------------------//
    [Fact]
    public void JsonPatchAddObjectMemberTest() {
        var caseData = """{"foo": "bar"}""".FromDb();

        var request = new PatchJsonPathRequest { Op = OperationType.Add, Path = "/baz", Value = "qux" };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);
        
        Assert.Equal(
            """{"foo": "bar", "baz": "qux"}""".ThroughDb(),
            patch.Result.ToDb());
    }
    
    [Fact]
    public void MergeAddObjectMemberTest() {
        var caseData = """{"foo": "bar"}""".FromDb();
        
        caseData.Merge(new { baz = "qux" }.ThroughHttp());
        
        Assert.Equal(
            """{"foo": "bar", "baz": "qux"}""".ThroughDb(),
            caseData.ToDb());
    }
    
    // ------------------------ A.2.  Adding an Array Element ------------------------//
    [Fact]
    public void JsonPatchAddArrayElementTest() {
        var caseData = """{ "foo": [ "bar", "baz" ] }""".FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Add, Path = "/foo/1", Value = "qux" };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.Equal(
            """{ "foo": [ "bar", "qux", "baz" ] }""".ThroughDb(),
            patch.Result.ToDb());
    }
    
    // ----------- Only adding at the end of the array is supported for now -----------//
    [Fact]
    public void MergeAddArrayElementTest() {
        var caseData = """{ "foo": [ "bar", "baz" ] }""".FromDb();
        
        caseData.Merge(new { foo = new[] {"qux"} }.ThroughHttp()); 
        
        Assert.Equal(
            """{ "foo": [ "bar", "baz", "qux" ] }""".ThroughDb(),
            caseData.ToDb());
    }
    
    
    // ------------------------ A.3.  Removing an Object Member ------------------------//
    [Fact]
    public void JsonPatchRemoveObjectMemberTest() {
        var caseData = """
            {
               "baz": "qux",
               "foo": "bar"
            }
            """
            .FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Remove, Path = "/baz" };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.Equal(
            """{ "foo": "bar" }""".ThroughDb(),
            patch.Result.ToDb());
    }
    
    [Fact]
    public void MergeRemoveObjectMemberTest() {
        var caseData = """
            {
                 "baz": "qux",
                 "foo": "bar"
            }
            """
            .FromDb();
        
        caseData.Merge(new { baz = (object)null! }.ThroughHttp()); 
        
        Assert.Equal(
            """{ "foo": "bar" }""".ThroughDb(),
            caseData.ToDb());
    }
    
    // ------------------------ A.4.  Removing an Array Element ------------------------//
    // ------------ Not supported by the Merge Api when array element is a JsonValue ------//
    [Fact]
    public void JsonPatchRemoveArrayElementTest() {
        var caseData = """{ "foo": [ "bar", "qux", "baz" ] }""".FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Remove, Path = "/foo/1" };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.Equal(
            """{ "foo": [ "bar", "baz" ] }""".ThroughDb(),
            patch.Result.ToDb());
    }
    
    // ------------------------ A.5.  Replacing a Value ------------------------//
    [Fact]
    public void JsonPatchReplaceValueTest() {
        var caseData = """
                       {
                         "baz": "qux",
                         "foo": "bar"
                       }
                       """.FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Replace, Path = "/baz", Value = "boo" };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.Equal(
            """
                {
                  "baz": "boo",
                  "foo": "bar"
                }
                """.ThroughDb(),
            patch.Result.ToDb());
    }
    
    [Fact]
    public void MergeReplaceValueTest() {
        var caseData = """
                       {
                         "baz": "qux",
                         "foo": "bar"
                       }
                       """.FromDb();
        
        caseData.Merge(new { baz = "boo" }.ThroughHttp());
        
        Assert.Equal(
            """
                {
                  "baz": "boo",
                  "foo": "bar"
                }
                """.ThroughDb(),
            caseData.ToDb());
    }
    
    // ------------------------ A.6.  Moving a Value ------------------------//
    [Fact]
    public void JsonPatchMoveValueTest() {
        var caseData = """
                       {
                         "foo": {
                           "bar": "baz",
                           "waldo": "fred"
                         },
                         "qux": {
                           "corge": "grault"
                         }
                       }
                       """.FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Move, From = "/foo/waldo", Path = "/qux/thud" };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.Equal(
            """
                {
                  "foo": {
                    "bar": "baz"
                  },
                  "qux": {
                    "corge": "grault",
                    "thud": "fred"
                  }
                }
                """.ThroughDb(),
            patch.Result.ToDb());
    }
    
    [Fact]
    public void MergeMoveValueTest() {
        var caseData = """
                       {
                         "foo": {
                           "bar": "baz",
                           "waldo": "fred"
                         },
                         "qux": {
                           "corge": "grault"
                         }
                       }
                       """.FromDb();
        
        caseData.Merge(new { foo = new {waldo = (string) null!}, qux = new { thud = "fred" } }.ThroughHttp());
        
        Assert.Equal(
            """
                {
                  "foo": {
                    "bar": "baz"
                  },
                  "qux": {
                    "corge": "grault",
                    "thud": "fred"
                  }
                }
                """.ThroughDb(),
            caseData.ToDb());
    }
    
    
    // ------------------------ A.7.  Moving an Array Element ------------------------//
    [Fact]
    public void JsonPatchMoveArrayElementTest() {
        var caseData = """{ "foo": [ "all", "grass", "cows", "eat" ] }""".FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Move, From = "/foo/1", Path = "/foo/3" };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.Equal(
            """{ "foo": [ "all", "cows", "eat", "grass" ] }""".ThroughDb(),
            patch.Result.ToDb());
    }
    
    // ------------ A.7.  You should reassign the whole array in 2 operations -------------------//
    [Fact]
    public void MergeMoveArrayElementTest() {
        var caseData = """{ "foo": [ "all", "grass", "cows", "eat" ] }""".FromDb();

        caseData.Merge( new { foo = (string)null! }.ThroughHttp());
        caseData.Merge( new { foo = new[] { "all", "cows", "eat", "grass" }}.ThroughHttp());
        
        Assert.Equal(
            """{ "foo": [ "all", "cows", "eat", "grass" ] }""".ThroughDb(),
            caseData.ToDb());
    }
    
    // ------------------------ A.8. Testing a Value: Success ------------------------//
    [Fact]
    public void JsonPatchTestingValueSuccessTest() {
        var caseData = """{ "baz": "qux" }""".FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Test, Path = "/baz", Value = "qux"};
        var result = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.True(result.IsSuccess);
    }
    
    // ------------------------ A.9. Testing a Value: Error ------------------------//
    [Fact]
    public void JsonPatchTestingValueErrorTest() {
        var caseData = """{ "baz": "qux" }""".FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Test, Path = "/baz", Value = "bar"};
        var result = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.False(result.IsSuccess);
    }
    
    // ------------------------ A.10.  Adding a Nested Member Object ------------------------//
    [Fact]
    public void JsonPatchAddNestedObjectMemberTest() {
        var caseData = """{ "foo": "bar" }""".FromDb();
        
        var request = new PatchJsonPathRequest {
            Op = OperationType.Add, Path = "/child", Value = new { grandchild = new {}}
        };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.Equal(
            """
                {
                  "foo": "bar",
                  "child": {
                    "grandchild": {
                    }
                  }
                }
                """.ThroughDb(),
            patch.Result.ToDb());
    }
    
    [Fact]
    public void MergeAddNestedObjectMemberTest() {
        var caseData = """{ "foo": "bar" }""".FromDb();

        caseData.Merge(new { child = new { grandchild = new {}} }.ThroughHttp());
        
        Assert.Equal(
            """
                {
                  "foo": "bar",
                  "child": {
                    "grandchild": {
                    }
                  }
                }
                """.ThroughDb(),
            caseData.ToDb());
    }
    
    // ------------------------ A.12.  Adding to a Nonexistent Target ------------------------//
    // ------------- Adding to whatever target is ALLOWED by design with the Merge API-------//
    [Fact]
    public void JsonPatchAddingANonExistentTargetTest() {
        var caseData = """{ "foo": "bar" }""".FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Add, Path = "/baz/bat", Value = "qux" };
        var result = new JsonPatch(request.ToPatchOperation()).Apply(caseData);
        
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public void MergeAddingANonExistentTargetTest() {
        var caseData = """{ "foo": "bar" }""".FromDb();

        caseData.Merge(new { baz = new { bat = "qux"}  }.ThroughHttp());
        
        Assert.Equal(
            """
                {
                  "foo": "bar",
                  "baz": {
                    "bat": "qux"
                  }
                }
                """.ThroughDb(),
            caseData.ToDb());
    }
    
    // ------------------------ A.14.  ~ Escape Ordering -----------------------------------------------//
    // ----------- non c# variables for keys are not supported by the Merge API ------------------------//
    [Fact]
    public void JsonPatchEscapeOrderingTest() {
        var caseData = """
                       {
                         "/": 9,
                         "~1": 10
                       }
                       """.FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Add, Path = "/~01", Value = 20 };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.Equal(
            """
                {
                  "/": 9,
                  "~1": 20
                }
                """.ThroughDb(),
            patch.Result.ToDb());
    }
    
    // ------------------------ A.16.  Adding an Array Value ----------------------------//
    // ------------- Merge API will always add to the end of the array ------------------//
    [Fact]
    public void JsonPatchAddArrayValueTest() {
        var caseData = """{ "foo": ["bar"] }""".FromDb();
        
        var request = new PatchJsonPathRequest { Op = OperationType.Add, Path = "/foo/-", Value = new List<string> { "abc", "def"} };
        var patch = new JsonPatch(request.ToPatchOperation()).Apply(caseData);

        Assert.Equal(
            """{ "foo": ["bar", ["abc", "def"]] }""".ThroughDb(),
            patch.Result.ToDb());
    }
    
    [Fact]
    public void MergeAddArrayValueTest() {
        var caseData = """{ "foo": ["bar"] }""".FromDb();

        caseData.Merge(new { foo =  new List<object> { new List<string> { "abc", "def"}} }.ThroughHttp());
        
        Assert.Equal(
            """{ "foo": ["bar", ["abc", "def"]] }""".ThroughDb(),
            caseData.ToDb());
    }
    

    [Fact]
    public void MergeAddTest() {
        var caseData = """{"name": "John"}""".FromDb();

        caseData.Merge(new { surname = "Doe", nested = new { id = 22 } }.ThroughHttp());

        Assert.Equal(
            """{"name": "John", "surname": "Doe", "nested": { "id": 22 }}""".ThroughDb(),
            caseData.ToDb());
    }

    [Fact]
    public void MergeNoOpTest() {
        var caseData = TestData().FromDb();

        caseData.Merge(new { }.ThroughHttp());
    
        Assert.Equal(TestData().ThroughDb(), caseData.ToDb());
    }
    
    [Fact]
    public void MergeReplaceValueWithArrayTest() {
        var caseData = """{"name": "John Doe"}""".FromDb();

        caseData.Merge(new { name = new List<string> { "one", "another" }}.ThroughHttp());

        Assert.Equal("""{"name": ["one", "another"]}""".ThroughDb(), caseData.ToDb());
    }
    
    [Fact]
    public void MergeReplaceObjectWithArrayFailsTest() {
        var caseData = """{"Person": { "Name": "John" } }""".FromDb();
        
        Assert.Throws<InvalidOperationException>(() =>
            caseData.Merge(new { Person = new List<string> { "one", "another" } }.ThroughHttp()));
    }

    [Fact]
    public void MergeObjectInitializerTest() {
        var caseData = """{"name": "John Doe"}""".FromDb();

        caseData.Merge(new { employee = new { Id = 22, Name = "Takis" } }.ThroughHttp());
        caseData.Merge(new { coordinates = new { Cartesian = new { x = 43, y = 5.05 } }}.ThroughHttp());

        Assert.Equal(
            """
                {
                  "name": "John Doe",
                  "employee": {
                    "Id": 22,
                    "Name": "Takis"
                  },
                  "coordinates": {
                    "Cartesian": {
                      "x": 43,
                      "y": 5.05
                    }
                  }
                }
                """.ThroughDb(),
            caseData.ToDb());
    }

    [Fact]
    public void StringNullWhitespaceEmptyTest() {
        var caseData = """{"name": "John", "surname": "Doe", "middle": "M."}""".ThroughHttp();
        caseData.Merge(JsonNode.Parse("""{"name": null}""")).ThroughHttp();
        caseData.Merge(new { surname = string.Empty }.ThroughHttp());
        caseData.Merge(new { middle = ' ' }.ThroughHttp());
        caseData.Merge(JsonNode.Parse("""{"father": null}""")).ThroughHttp();
        caseData.Merge(new { mother = string.Empty }.ThroughHttp());
        caseData.Merge(new { grandpapa = ' ' }.ThroughHttp());
    
        Assert.Equal(
            """
                {
                 "surname": "",
                 "middle": " ",
                 "mother": "",
                 "grandpapa": " "
                }
                """.ThroughDb(),
            caseData.ToDb());
    }

    [Fact]
    public void JsonTypesAreRespectedWhenMutatingTest() {
        var caseData = """{"name": "John Doe"}""".FromDb();
        
        caseData.Merge(new { trueValue = true }.ThroughHttp());
        caseData.Merge(new { falseValue = false }.ThroughHttp());
        caseData.Merge(new { nullValue = (string)null! }.ThroughHttp());
        caseData.Merge(new { emptyValue = string.Empty }.ThroughHttp());
        caseData.Merge(new { intValue = 3 }.ThroughHttp());
        caseData.Merge(new { decimalValue = 2.1m }.ThroughHttp());
        caseData.Merge(new { doubleValue = 1.3 }.ThroughHttp());
        caseData.Merge(new { arrayValue = new List<int> { 1, 2 } }.ThroughHttp());
        caseData.Merge(new {
            arrayOfObjects = new List<object> { new { Object = "test" }, new { Object = "test2" } }
        }.ThroughHttp());


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
                """.ThroughDb(),
            caseData.ToDb());
    }

    // -----------------------Replacing nested object in arrays is not supported-----------------------//
    // e.g. in the following example "Title" could be our key - or an "Id", which we could deduce as a "replace"
    // action. An implementation for this exists upon request but it has not been tested thoroughly.
    // Just use JsonPatch if you have this business case.
    // [Fact]
    // public void ReplaceNestedObjectInArrayTest() {
    //     var caseData = """
    //            {
    //              "Books": [
    //                { "Title": "Anna Karenina", "Author": "Leo Tolstoy", "Grade": "A" },
    //                { "Title": "War and Peace", "Author": "Leo Tolstoy", "Grade": "A" }
    //              ]
    //            }
    //    """.ToJsonNode();
    //     
    //     caseData.Merge(new { Books = new List<object> {
    //         new { Title = "War and Peace", Author = "Leo Tolstoy", Grade = "A+" }
    //     } }.ThroughHttp());
    //
    //     Assert.Equal("""
    //             {
    //               "Books": [
    //                 { "Title": "Anna Karenina", "Author": "Leo Tolstoy", "Grade": "A" },
    //                 { "Title": "War and Peace", "Author": "Leo Tolstoy", "Grade": "A+" }
    //               ]
    //             }
    //    """.ThroughConverter(), caseData.FromJsonNode());
    // }
    
    [Fact]
    public void MergeMultiplePatchesInOneActionTest() {
        var caseData = """
                       {
                         "Name": "John Doe",
                         "Books": [
                           {
                             "AnnaKarenina": "Perfect"
                           }
                         ],
                         "Address": {
                           "Location": {
                             "Name": "Somewhere",
                             "Cities": ["Athens", "Levadia"]
                           }
                         }
                       }
                       """.FromDb();

        caseData.Merge(new {
            Name = "John Rambo",
            Books = new List<object> { new { LenaManta = "Poor" } },
            Id = 5,
            Address = new { Location = 
                new { Cities = new List<object> { new { Name = "Neverland", Coordinates = "38° 26′ 0″ N, 22° 52′ 0″ E" } } }
            }
        }.ThroughHttp());
        
        Assert.Equal("""
                       {
                         "Name": "John Rambo",
                         "Books": [
                            {
                              "AnnaKarenina": "Perfect"
                            },
                            {
                              "LenaManta": "Poor"
                            }
                          ],
                         "Address": {
                             "Location": {
                                 "Name": "Somewhere",
                                 "Cities": [
                                     "Athens",
                                     "Levadia",
                                     {
                                       "Name": "Neverland",
                                       "Coordinates": "38° 26′ 0″ N, 22° 52′ 0″ E"
                                     }
                                 ]
                             }
                         },
                         "Id": 5
                     }
                     """.ThroughDb(), caseData.ToDb());
    }
    
    [Fact]
    public void JsonPatchNestedPropertiesTest() {
        var caseData = """
                       {
                            "Name": "John Doe",
                            "Hobbies": [
                              {
                                "Books": {
                                  "ArtOfWar": "1"
                                }
                              }
                            ],
                            "Address": {
                              "Location": {
                                "Name": "Somewhere",
                                "Cities": ["Athens", "Levadia"]
                              }
                            }
                       }
                       """.FromDb();
        
        var request = new List<PatchJsonPathRequest> {
            new() { Op = OperationType.Add, Path = "/Name", Value = "John Rambo" },
            new() { Op = OperationType.Add, Path = "/Hobbies/-", Value = new { Books = new { LenaManta = "2" } } },
            new() { Op = OperationType.Add, Path = "/Address/Location/Name", Value = "TestLocation" },
            new() { Op = OperationType.Add, Path = "/Address/Location/Id", Value = 2 },
            new() { Op = OperationType.Add, Path = "/Id", Value = 5 },
            new() { Op = OperationType.Add, Path = "/Address/Location/Cities/-", Value = "Orxomenos" },
            new() { Op = OperationType.Add, Path = "/Address/Location/Cities/-",
                Value = new { Name = "Neverland", Coordinates = "38° 26′ 0″ N, 22° 52′ 0″ E" }
            },
        };
        var operations = request.Select(op => op.ToPatchOperation()).ToList();
        var patch = new JsonPatch(operations).Apply(caseData);
        
        Assert.Equal("""
                     {
                       "Name": "John Rambo",
                       "Hobbies": [
                           {
                               "Books": {
                                   "ArtOfWar": "1"
                               }
                           },
                           {
                             "Books": {
                               "LenaManta": "2"
                             }
                           }
                       ],
                       "Address": {
                           "Location": {
                               "Name": "TestLocation",
                               "Cities": [
                                   "Athens",
                                   "Levadia",
                                   "Orxomenos",
                                   {
                                     "Name": "Neverland",
                                     "Coordinates": "38° 26′ 0″ N, 22° 52′ 0″ E"
                                   }
                               ],
                               "Id": 2
                           }
                       },
                       "Id": 5
                     }
                     """.ThroughDb(), patch.Result.ToDb());
    }

    [Fact]
    public void MergeAddRemoveInOneActionTest() {
        var caseData = """
                       {
                         "hobbies": [
                           {
                             "name": "coding"
                           }
                         ],
                         "martialArts": [
                           {
                             "name": "jiu jitsu"
                           }
                         ],
                         "cities": [
                           {
                             "name": "Levadia"
                           }
                         ]
                       }
                       """.FromDb();

        caseData.Merge(new {
            martialArts = new List<object> { new { name = "karate" } },
            cities = (string)null!
        }.ThroughHttp());
    
        Assert.Equal(
            """
                {
                  "hobbies": [
                      {
                        "name": "coding"
                      }
                    ],
                   "martialArts": [
                    {
                       "name": "jiu jitsu"
                     },
                     {
                       "name": "karate"
                     }
                   ]
                 }
                """.ThroughDb(),
            caseData.ToDb());
    }
    
    [Fact]
    public void JsonPatchAddRemoveReplaceTypeInOneActionTest() {
        var caseData = """
                       {
                         "hobbies": [
                           {
                             "name": "coding"
                           }
                         ],
                         "martialArts": [
                           {
                             "name": "jiu jitsu"
                           }
                         ],
                         "cities": [
                           {
                             "name": "Levadia"
                           }
                         ]
                       }
                       """.FromDb();
        
        var request = new List<PatchJsonPathRequest> {
            new() { Op = OperationType.Add, Path = "/hobbies", Value = new { newName = new List<object> { new { name = "working" } } } },
            new() { Op = OperationType.Add, Path = "/martialArts/-", Value = new { name = "karate"  } },
            new() { Op = OperationType.Remove, Path = "/cities" },
        };
        var operations = request.Select(op => op.ToPatchOperation()).ToList();
        var patch = new JsonPatch(operations).Apply(caseData);
        
        Assert.Equal(
            """
                {
                 "hobbies": {
                   "newName": [
                     {
                       "name": "working"
                     }
                   ]
                  },
                  "martialArts": [
                   {
                      "name": "jiu jitsu"
                    },
                    {
                      "name": "karate"
                    }
                  ]
                }
                """.ThroughDb(),
            patch.Result.ToDb());
    }
    
    [Fact]
    public void MergeWorkInOrderTest() {
        var caseData = """
                       {
                         "name": "John Doe",
                         "hobbies": [
                           {
                             "name": "coding"
                           },
                           {
                             "name": "casing"
                           }
                         ],
                         "address": {
                           "location": {
                             "street": "Analipseos 1",
                             "neighborhood": "Zagaras"
                           }
                         }
                       }
                       """.FromDb();

        caseData.Merge(new {
            name = (string)null!,
            title = "Mister",
            wife = new { name = "elsa" },
            address = new { location = new { postalCode = 32100} },
            hobbies = new List<object> { new { name = new { first = "Ping", second = "Pong" } } },
        }.ThroughHttp());
    
        Assert.Equal("""
                       {
                            "hobbies": [
                              {
                                "name": "coding"
                              },
                              {
                                "name": "casing"
                              },
                              {
                                "name": {
                                  "first": "Ping",
                                  "second": "Pong"
                                }
                              }
                            ],
                            "address": {
                              "location": {
                                "street": "Analipseos 1",
                                "neighborhood": "Zagaras",
                                "postalCode": 32100
                              }
                            },
                            "title": "Mister",
                            "wife": {
                              "name": "elsa"
                            }
                     }
                     """.ThroughDb(), caseData.ToDb());
    }

    public string TestData() {
        return """
                           {
                                "name": "John Doe",
                                "hobbies": [
                                  {
                                    "name": "coding"
                                  },
                                  {
                                    "name": "casing"
                                  }
                                ],
                                "address": {
                                  "location": {
                                    "street": "Analipseos 1",
                                    "neighborhood": "Zagaras"
                                  }
                                }
                           }
               """;
    }
}