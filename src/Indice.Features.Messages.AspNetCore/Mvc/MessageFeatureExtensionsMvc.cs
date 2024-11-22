using Indice.AspNetCore.Mvc.ApplicationModels;
using Indice.Events;
using Indice.Features.Messages.AspNetCore.Authorization;
using Indice.Features.Messages.AspNetCore.Mvc;
using Indice.Features.Messages.AspNetCore.Mvc.Formatters;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Contains extension methods on <see cref="IMvcBuilder"/> for configuring Campaigns API feature.</summary>
public static class MessageFeatureExtensionsMvc
{
    /// <summary>Adds all Messages (both management and self-service) API endpoints in the MVC project.</summary>
    /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
    /// <param name="configureAction">Configuration for several options of Campaigns API feature.</param>
    public static IMvcBuilder AddMessageEndpoints(this IMvcBuilder mvcBuilder, Action<MessageEndpointOptions> configureAction = null) {
        var services = mvcBuilder.Services;
        // Configure options.
        var apiOptions = new MessageEndpointOptions(services);
        configureAction?.Invoke(apiOptions);

        return mvcBuilder.AddMessageManagementEndpoints(options => {
            options.PathPrefix = apiOptions.PathPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.RequiredScope = apiOptions.RequiredScope;
            options.UserClaimType = apiOptions.UserClaimType;
            options.GroupName = apiOptions.ManagementGroupName;
        })
        .AddMessageInboxEndpoints(options => {
            options.PathPrefix = apiOptions.PathPrefix;
            options.ConfigureDbContext = apiOptions.ConfigureDbContext;
            options.DatabaseSchema = apiOptions.DatabaseSchema;
            options.UserClaimType = apiOptions.UserClaimType;
            options.GroupName = apiOptions.InboxGroupName;
        });
    }

    /// <summary>Adds Messages management API endpoints in the MVC project.</summary>
    /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
    /// <param name="configureAction">Configuration for several options of Campaigns management API feature.</param>
    public static IMvcBuilder AddMessageManagementEndpoints(this IMvcBuilder mvcBuilder, Action<MessageManagementOptions> configureAction = null) {


        mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new MessageFeatureProvider(includeManagementApi: true, includeInboxApi: false)));
        var services = mvcBuilder.Services;
        // Register framework services.
        services.AddMessageManagement(configureAction);
        services.AddSingleton<IConfigureOptions<MvcOptions>, MessageManagementConfigureMvcOptions>();
        return mvcBuilder;
    }

    /// <summary>Adds Messages inbox API endpoints in the MVC project.</summary>
    /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
    /// <param name="configureAction">Configuration for several options of Campaigns inbox API feature.</param>
    public static IMvcBuilder AddMessageInboxEndpoints(this IMvcBuilder mvcBuilder, Action<MessageInboxOptions> configureAction = null) {
        mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new MessageFeatureProvider(includeManagementApi: false, includeInboxApi: true)));
        var services = mvcBuilder.Services;
        // Register framework services.
        services.AddMessageInbox(configureAction);
        services.AddSingleton<IConfigureOptions<MvcOptions>, MessageInboxConfigureMvcOptions>();
        return mvcBuilder;
    }
}
