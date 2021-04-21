using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Swagger;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.Identity.Configuration
{
    public class CreateUserRequestSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (context.Type == typeof(CreateUserRequest)) {
                var example = new CreateUserRequest {
                    FirstName = "Georgios",
                    LastName = "Manoltzas",
                    Email = "g.manoltzas@indice.gr",
                    PhoneNumber = "6992731575",
                    UserName = "gmanoltzas",
                    ChangePasswordAfterFirstSignIn = true
                };
                schema.Example = example.ToOpenApiAny();
            }
        }
    }
}
