using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.AspNetCore.Features
{
    /// <summary>
    /// Avatar feature implementation for <see cref="IApplicationFeatureProvider{ControllerFeature}"/>.
    /// </summary>
    public class AvatarFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        /// <summary>
        /// Populates the feature for the current ASP.NET app.
        /// </summary>
        /// <param name="parts">The list of <see cref="ApplicationPart"/> instances in the application.</param>
        /// <param name="feature">The feature instance to populate.</param>
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
            var type = typeof(AvatarController).GetTypeInfo();
            if (!feature.Controllers.Any(x => x == type)) {
                feature.Controllers.Add(type);
            }
        }
    }
}
