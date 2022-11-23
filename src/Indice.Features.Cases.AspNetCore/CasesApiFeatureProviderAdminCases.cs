using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Indice.Features.Cases.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.Features.Cases
{
    /// <summary>
    /// Provider for registering admin-cases features.
    /// </summary>
    internal class CasesApiFeatureProviderAdminCases : IApplicationFeatureProvider<ControllerFeature>
    {
        private static IReadOnlyList<TypeInfo> ControllerTypes => new List<TypeInfo> {
            typeof(AdminCasesController).GetTypeInfo(),
            typeof(AdminWorkflowInvokerController).GetTypeInfo(),
            typeof(AdminAttachmentsController).GetTypeInfo(),
            typeof(AdminUsersController).GetTypeInfo(),
            typeof(AdminCaseTypesController).GetTypeInfo(),
            typeof(AdminCheckpointTypesController).GetTypeInfo(),
            typeof(AdminIntegrationController).GetTypeInfo(),
            typeof(LookupController).GetTypeInfo(),
            typeof(AdminFiltersController).GetTypeInfo(),
        };

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
            foreach (var type in ControllerTypes) {
                if (!feature.Controllers.Any(x => x.FullName == type.FullName)) {
                    feature.Controllers.Add(type);
                }
            }
        }
    }
}