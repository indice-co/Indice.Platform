using System;
using System.Collections.Generic;
using System.Text;
using Indice.AspNetCore.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds feature extensions to the MvcBuilder.
    /// </summary>
    public static class AvatarFeatureExtensions
    {
        /// <summary>
        /// Add the Avatar feature to MVC.
        /// </summary>
        /// <param name="mvcBuilder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddAvatars(this IMvcBuilder mvcBuilder) {
            mvcBuilder.ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new AvatarFeatureProvider()));

            mvcBuilder.Services.AddResponseCaching();
            return mvcBuilder;
        }
    }
}
