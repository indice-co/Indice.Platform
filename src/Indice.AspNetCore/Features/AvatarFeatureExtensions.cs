using System;
using System.Collections.Generic;
using System.Text;
using Indice.AspNetCore.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AvatarFeatureExtensions
    {

        public static IMvcBuilder AddAvatars(this IMvcBuilder mvcBuilder) {
            mvcBuilder.ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new AvatarFeatureProvider()));

            mvcBuilder.Services.AddResponseCaching();
            return mvcBuilder;
        }
    }
}
