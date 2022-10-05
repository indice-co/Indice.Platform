using System;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Features.Multitenancy.Core;
using Indice.Serialization;
using Indice.Types;
using Microsoft.Azure.Functions.Worker;

namespace Indice.Features.Multitenancy.Worker.Azure.Strategies
{
    internal class FromQueueTriggerPayloadResolutionStrategy : ITenantResolutionStrategy
    {
        private readonly IFunctionContextAccessor _functionContextAccessor;

        public FromQueueTriggerPayloadResolutionStrategy(IFunctionContextAccessor functionContextAccessor) {
            _functionContextAccessor = functionContextAccessor ?? throw new ArgumentNullException(nameof(functionContextAccessor));
        }

        public async Task<string> GetTenantIdentifierAsync() {
            var message = _functionContextAccessor.FunctionContext.GetInputData<ReadOnlyMemory<byte>>("message");
            var originalMessage = await CompressionUtils.Decompress(message.ToArray());
            var envelope = JsonSerializer.Deserialize<DefaultEnvelope>(originalMessage, JsonSerializerOptionDefaults.GetDefaultSettings());
            return envelope.TenantId?.ToString();
        }

        private class DefaultEnvelope : EnvelopeBase { }
    }
}
