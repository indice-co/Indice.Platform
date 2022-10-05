using Microsoft.Azure.Functions.Worker.Converters;

namespace Indice.Features.Messages.Worker.Azure
{
    /// <summary></summary>
    public class MessagesInputConverter : IInputConverter
    {
        /// <inheritdoc />
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context) => ValueTask.FromResult(ConversionResult.Unhandled());
    }
}
