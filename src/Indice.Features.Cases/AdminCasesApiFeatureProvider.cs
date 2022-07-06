using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Indice.Features.Cases.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.Features.Cases
{
    internal class AdminCasesApiFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private static IReadOnlyList<TypeInfo> ControllerTypes => new List<TypeInfo>() {
            typeof(AdminCasesController).GetTypeInfo(),
            typeof(AdminApprovalsController).GetTypeInfo(),
            typeof(AdminEditController).GetTypeInfo(),
            typeof(AdminAssignmentsController).GetTypeInfo(),
            typeof(AdminAttachmentsController).GetTypeInfo(),
            typeof(AdminUsersController).GetTypeInfo(),
            typeof(AdminCaseTypesController).GetTypeInfo(),
            typeof(AdminIntegrationController).GetTypeInfo(),
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