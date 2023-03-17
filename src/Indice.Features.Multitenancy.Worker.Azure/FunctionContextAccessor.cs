/* 
 * Attribution: https://gist.github.com/dolphinspired/796d26ebe1237b78ee04a3bff0620ea0 
 */

using Microsoft.Azure.Functions.Worker;

namespace Indice.Features.Multitenancy.Worker.Azure;

/// <summary>The default implementation of <see cref="IFunctionContextAccessor"/>.</summary>
public class FunctionContextAccessor : IFunctionContextAccessor
{
    private static readonly AsyncLocal<FunctionContextRedirect> _currentContext = new();

    /// <inheritdoc />
    public virtual FunctionContext FunctionContext {
        get => _currentContext.Value?.HeldContext;
        set {
            var holder = _currentContext.Value;
            if (holder is not null) {
                // Clear current context trapped in the AsyncLocals, as its done.
                holder.HeldContext = null;
            }
            if (value is not null) {
                // Use an object indirection to hold the context in the AsyncLocal, so it can be cleared in all ExecutionContexts when its cleared.
                _currentContext.Value = new FunctionContextRedirect {
                    HeldContext = value
                };
            }
        }
    }

    private class FunctionContextRedirect
    {
        public FunctionContext HeldContext;
    }
}
