using System.Text.Json;
using Indice.Serialization;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Indice.Features.Messages.Worker.Azure
{
    /// <summary></summary>
    public class MessagesInputConverter : IInputConverter
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();

        /// <inheritdoc />
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context) {
            if (context.Source is string source) {
                var message = JsonSerializer.Deserialize(source, context.TargetType, JsonSerializerOptions);
                var result = ConversionResult.Success(message);
                return ValueTask.FromResult(result);
            }
            return ValueTask.FromResult(ConversionResult.Unhandled());
        }
    }
}
