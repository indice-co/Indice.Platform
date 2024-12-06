using System.Text.Json;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Tests;

[Trait("CaseManagement", "SchemaValidatorService")]
public class SchemaValidatorTests
{
    private readonly ISchemaValidator _schemaValidator = new CasesJsonSchemaValidator();
    private const string Schema = @"{
	    ""title"": ""Test"",
	    ""type"": ""object"",
	    ""properties"": {
		    ""firstName"": {
			    ""type"": ""string""
		    },
		    ""lastName"": {
			    ""type"": ""string""
		    }
	    },
	    ""required"": [
		    ""firstName""
	    ]
    }";

    private const string SchemaArray = @"{
        ""type"": ""array"",
        ""minItems"": 1,
        ""maxItems"": 1,
        ""items"": {
            ""properties"": {
                ""attachmentId"": {
                    ""type"": ""string""
                }
            },
            ""required"": [
                ""attachmentId""
            ]
        }
    }"; 
 
    [Fact]
    public void IsValid_EmptySchema_Throws() {
        Assert.Throws<ArgumentNullException>(() => _schemaValidator.IsValid(It.IsAny<string>(), It.IsAny<string>()));
    }

    [Fact]
    public void IsValid_EmptyData_Throws() {
        Assert.Throws<ArgumentNullException>(() => _schemaValidator.IsValid(Schema, It.IsAny<string>()));
    }

    [Fact]
    public void IsValid_EmptyObject_False() {
        var result = _schemaValidator.IsValid(Schema, "{}");
        Assert.False(result);
    }

    [Fact]
    public void IsValid_NotValidObject_False() {
        var result = _schemaValidator.IsValid(Schema, "{\"lastName\": \"doe\"}");
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ValidObject_StringData_True() {
        var result = _schemaValidator.IsValid(Schema, "{\"firstName\": \"john\",\"lastName\": \"doe\"}");
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ValidObject_JsonElementData_True() {
        var data = JsonSerializer.Deserialize<JsonElement>("{\"firstName\": \"john\",\"lastName\": \"doe\"}");
        var result = _schemaValidator.IsValid(Schema, data);
        Assert.True(result);
    }
    
    [Fact]
    public void IsValid_ValidObject_JObjectData_True() {
        var data = Newtonsoft.Json.Linq.JObject.Parse("{\"firstName\": \"john\",\"lastName\": \"doe\"}");
        var result = _schemaValidator.IsValid(Schema, data);
        Assert.True(result);
    }
    
    [Fact]
    public void IsValid_NotValidObject_JArrayData_False() {
        var data = Newtonsoft.Json.Linq.JArray.Parse("[{\"propertyA\":\"123\"}]");
        var result = _schemaValidator.IsValid(SchemaArray, data);
        Assert.False(result);
    } 
    
    [Fact]
    public void IsValid_ValidObject_JArrayData_True() {
        var data = Newtonsoft.Json.Linq.JArray.Parse("[{\"attachmentId\":\"123\"}]");
        var result = _schemaValidator.IsValid(SchemaArray, data);
        Assert.True(result);
    }
}