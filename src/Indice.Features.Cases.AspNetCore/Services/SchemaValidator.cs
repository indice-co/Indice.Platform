using System.Text.Json;
using Indice.Features.Cases.Interfaces;
using Json.Schema;

namespace Indice.Features.Cases.Services
{
    internal class SchemaValidator : ISchemaValidator
    {
        public bool IsValid(string schema, string data) {
            if (string.IsNullOrEmpty(schema)) throw new ArgumentNullException(nameof(schema));
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));

            var mySchema = JsonSchema.FromText(schema);
            var json = JsonDocument.Parse(data);

            var validate = mySchema.Validate(json.RootElement, new ValidationOptions {
                OutputFormat = OutputFormat.Verbose
            });

            return validate.IsValid;
        }
    }
}