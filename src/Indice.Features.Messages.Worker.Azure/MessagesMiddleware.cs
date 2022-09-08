using Indice.Types;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Indice.Features.Messages.Worker.Azure
{
    /// <summary></summary>
    public class MessagesMiddleware : IFunctionsWorkerMiddleware
    {
        /// <inheritdoc />
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next) {
            var queueTriggerBinding = context.FunctionDefinition.InputBindings.Values.FirstOrDefault(binding => binding.Type == "queueTrigger");
            if (queueTriggerBinding is not null) {
                var bindingResult = await context.BindInputAsync<DefaultEnvelope>(queueTriggerBinding);
            }
            await next(context);
        }
    }

    /// <inheritdoc />
    public class DefaultEnvelope : EnvelopeBase { }
}
