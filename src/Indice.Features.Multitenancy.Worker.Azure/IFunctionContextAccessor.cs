using Microsoft.Azure.Functions.Worker;

namespace Indice.Features.Multitenancy.Worker.Azure
{
    /// <summary>Provides access to the current <see cref="Microsoft.Azure.Functions.Worker.FunctionContext"/>.</summary>
    public interface IFunctionContextAccessor
    {
        /// <summary>Encapsulates the information about a function execution.</summary>
        FunctionContext FunctionContext { get; set; }
    }
}
