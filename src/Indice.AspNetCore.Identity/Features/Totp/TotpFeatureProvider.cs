using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Indice.AspNetCore.Identity.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Indice.AspNetCore.Features
{
    /// <summary>
    /// TOTP feature implementation for <see cref="IApplicationFeatureProvider{ControllerFeature}"/>.
    /// </summary>
    public class TotpFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        /// <summary>
        /// Populates the feature for the current ASP.NET Core app.
        /// </summary>
        /// <param name="parts">The list of <see cref="ApplicationPart"/> instances in the application.</param>
        /// <param name="feature">The feature instance to populate.</param>
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
            var type = typeof(TotpController).GetTypeInfo();
            if (!feature.Controllers.Any(x => x == type)) {
                feature.Controllers.Add(type);
            }
        }
    }
}
