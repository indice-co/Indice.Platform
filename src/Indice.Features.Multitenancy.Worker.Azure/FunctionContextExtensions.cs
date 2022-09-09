using System.Collections.Generic;
using System.Linq;
using Indice.Features.Multitenancy.Core;
using Indice.Features.Multitenancy.Worker.Azure;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>Extension methods on <see cref="FunctionContext"/> class.</summary>
    public static class FunctionContextExtensions
    {
        /// <summary>Returns the current tenant.</summary>
        /// <typeparam name="TTenant">The type of the tenant.</typeparam>
        /// <param name="context">Encapsulates the information about a function execution.</param>
        public static TTenant GetTenant<TTenant>(this FunctionContext context) where TTenant : Tenant {
            if (!context.Items.ContainsKey(Constants.FunctionContextTenantKey)) {
                return default;
            }
            return context.Items[Constants.FunctionContextTenantKey] as TTenant;
        }

        /// <summary>Returns the current tenant.</summary>
        /// <param name="context">Encapsulates the information about a function execution.</param>
        public static Tenant GetTenant(this FunctionContext context) => context.GetTenant<Tenant>();

        /// <summary>Gets the bound data argument with the given name.</summary>
        /// <typeparam name="TArgumentType"></typeparam>
        /// <param name="context">Encapsulates the information about a function execution.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static TArgumentType GetInputData<TArgumentType>(this FunctionContext context, string argumentName) {
            (var featureType, var featureInstance) = context.Features.SingleOrDefault(kvp => kvp.Key.Name == "IFunctionBindingsFeature");
            var inputData = featureType.GetProperties().SingleOrDefault(property => property.Name == "InputData")?.GetValue(featureInstance) as IReadOnlyDictionary<string, object>;
            if (inputData?.ContainsKey(argumentName) == true) {
                return (TArgumentType)inputData[argumentName];
            }
            return default;
        }
    }
}
