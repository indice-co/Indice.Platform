using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.AspNetCore.Identity.Api
{
    /// <summary>
    /// Push notifications feature implementation for <see cref="IApplicationFeatureProvider{ControllerFeature}"/>.
    /// </summary>
    public class DevicesFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private static IReadOnlyList<TypeInfo> ControllerTypes => new List<TypeInfo>() {
            typeof(DevicesController).GetTypeInfo()
        };

        /// <summary>
        /// Populates the feature for the current ASP.NET Core app.
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
