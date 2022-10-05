using System.Text.Json;
using Indice.Features.Cases.Interfaces;
using Json.Schema;

namespace Indice.Features.Cases.Services
{
    internal class SchemaValidator : ISchemaValidator
    {
        public bool IsValid(string schema, string data) {
            var mySchema = JsonSchema.FromText(schema);

            var json = JsonDocument.Parse(data);

            var validate = mySchema.Validate(json.RootElement, new ValidationOptions() {
                OutputFormat = OutputFormat.Verbose
            });

            return validate.IsValid;
        }
    }
}