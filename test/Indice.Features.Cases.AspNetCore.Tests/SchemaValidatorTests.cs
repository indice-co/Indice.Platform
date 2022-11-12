using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Services;

namespace Indice.Features.Cases.Tests;

[Trait("CaseManagement", "SchemaValidatorService")]
public class SchemaValidatorTests
{
    private readonly ISchemaValidator _schemaValidator = new SchemaValidator();
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

    [Fact]
    public void IsValid_EmptySchema_True(){
        var result = _schemaValidator.IsValid(It.IsAny<string>(), It.IsAny<string>());
        Assert.True(result);
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
    public void IsValid_ValidObject_True() {
        var result = _schemaValidator.IsValid(Schema, "{\"firstName\": \"john\",\"lastName\": \"doe\"}");
        Assert.True(result);
    }
}