using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Indice.AspNetCore.Identity.Api.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.AspNetCore.Identity.Api
{
    /// <summary>
    /// A provider for <see cref="ControllerFeature"/>.
    /// </summary>
    internal class IdentityServerApiFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private static IReadOnlyList<TypeInfo> ControllerTypes => new List<TypeInfo>() {
            typeof(MyAccountController).GetTypeInfo(),
            typeof(ClaimTypesController).GetTypeInfo(),
            typeof(ClientsController).GetTypeInfo(),
            typeof(DashboardController).GetTypeInfo(),
            typeof(ResourcesController).GetTypeInfo(),
            typeof(RolesController).GetTypeInfo(),
            typeof(UsersController).GetTypeInfo(),
            typeof(LookupsController).GetTypeInfo()
        };

        /// <summary>
        /// Updates the feature instance by adding the IdentityServer API controllers.
        /// </summary>
        /// <param name="parts">The list of <see cref="ApplicationPart"/> instances in the application.</param>
        /// <param name="feature">The feature instance to populate.</param>
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
            foreach (var type in ControllerTypes) {
                if (!feature.Controllers.Any(x => x.FullName == type.FullName)) {
                    feature.Controllers.Add(type);
                }
            }
        }
    }
}
