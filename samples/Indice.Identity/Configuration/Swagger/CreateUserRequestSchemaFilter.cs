using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Swagger;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.Identity.Configuration;

public class CreateUserRequestSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
        if (context.Type == typeof(CreateUserRequest)) {
            schema.Example = new CreateUserRequest {
                FirstName = "Georgios",
                LastName = "Manoltzas",
                Email = "g.manoltzas@indice.gr",
                PhoneNumber = "6992731575",
                UserName = "gmanoltzas",
                ChangePasswordAfterFirstSignIn = true,
                Claims = new System.Collections.Generic.List<BasicClaimInfo> {
                    new BasicClaimInfo {
                        Type = IdentityModel.JwtClaimTypes.Locale,
                        Value = "el"
                    }
                }
            }
            .ToOpenApiAny();
            //schema.Example = new {
            //    Test = "the string",
            //    Age = 23,
            //    Date = System.DateTimeOffset.Now,
            //    Dictionary = new Dictionary<string, CreateUserRequest> {
            //        ["One"] = example
            //    },
            //    YetAnotherDictionary = new TranslationDictionary<string> {
            //        ["en"] = "Yes"
            //    },
            //    Test2 = new Dictionary<string, int> {
            //        ["The"] = 1,
            //        ["First"] = 2,
            //        ["Thing"] = 2,
            //    }
            //}
            //.ToOpenApiAny();
        }
    }
}
