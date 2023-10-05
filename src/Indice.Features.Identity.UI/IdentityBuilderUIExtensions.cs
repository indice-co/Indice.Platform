using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Indice.Features.Identity.UI;
using Indice.Features.Identity.UI.Assets;
using Indice.Features.Identity.UI.Localization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IServiceCollection"/> for registering required services for Identity UI pages feature.</summary>
public static class IdentityBuilderUIExtensions
{
    /// <summary>Adds the required services and pages for Identity UI pages features.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configureAction">Configure action.</param>
    public static IServiceCollection AddIdentityUI(this IServiceCollection services, IConfiguration configuration, Action<IdentityUIOptions>? configureAction = null) {
        services.PostConfigure<IdentityUIOptions>(options => configureAction?.Invoke(options));
        services.PostConfigure<AntiforgeryOptions>(options => options.HeaderName = "X-XSRF-TOKEN");
        // Post configure razor pages options.
        services.ConfigureOptions(typeof(IdentityUIRazorPagesConfigureOptions));
        services.ConfigureOptions(typeof(IdentityUIStaticFileConfigureOptions));
        // Configure FluentValidation.
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddFluentValidationAutoValidation(config => {
            config.DisableDataAnnotationsValidation = true;
        });
        services.AddFluentValidationClientsideAdapters();
        // Configure required localization services.
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddMvcCore()
                .ConfigureApplicationPartManager(apm => {
                    // We try to resolve the UI framework that was used by looking at the entry assembly.
                    // When an app runs, the entry assembly will point to the built app. In some rare cases (functional testing) the app assembly will be different, and we'll try to locate it through the same mechanism that MVC uses today.
                    // Finally, if for some reason we aren't able to find the assembly, we'll use our default value (Bootstrap5).
                    if (!TryResolveUIFramework(Assembly.GetEntryAssembly(), out var framework) &&
                        !TryResolveUIFramework(GetApplicationAssembly(services), out framework)) {
                        framework = default;
                    }
                    var parts = new ConsolidatedAssemblyApplicationPartFactory().GetApplicationParts(typeof(IdentityBuilderUIExtensions).Assembly);
                    foreach (var part in parts) {
                        apm.ApplicationParts.Add(part);
                    }
                    apm.FeatureProviders.Add(new ViewVersionFeatureProvider(framework));
                })
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix, options => options.ResourcesPath = "Resources");
        services.AddTransient<IIdentityViewLocalizer, IdentityViewLocalizer>();
        // Add default (system) implementation of zoneInfoProvider
        services.AddZoneInfoProvider();
        // Configure other services.
        services.AddGeneralSettings(configuration);
        services.AddMarkdown();
        return services;
    }

    private static Assembly? GetApplicationAssembly(IServiceCollection services) {
        // This is the same logic that MVC follows to find the application assembly.
        var environment = services.Where(d => d.ServiceType == typeof(IWebHostEnvironment)).ToArray();
        var applicationName = ((IWebHostEnvironment?)environment.LastOrDefault()?.ImplementationInstance)
            ?.ApplicationName;

        if (applicationName == null) {
            return null;
        }
        var appAssembly = Assembly.Load(applicationName);
        return appAssembly;
    }

    private static bool TryResolveUIFramework(Assembly? assembly, out UIFramework uiFramework) {
        uiFramework = default;

        var metadata = assembly?.GetCustomAttributes<UIFrameworkAttribute>()
            .SingleOrDefault()?.UIFramework; // Bootstrap4 is the default
        if (metadata == null) {
            return false;
        }

        // If we find the metadata there must be a valid framework here.
        if (!Enum.TryParse(metadata, ignoreCase: true, out uiFramework)) {
            var enumValues = string.Join(", ", Enum.GetNames(typeof(UIFramework)).Select(v => $"'{v}'"));
            throw new InvalidOperationException(
                $"Found an invalid value for the 'IdentityUIFrameworkVersion'. Valid values are {enumValues}");
        }

        return true;
    }

    internal sealed class ViewVersionFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
    {
        private readonly UIFramework _framework;

        public ViewVersionFeatureProvider(UIFramework framework) => _framework = framework;

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature) {
            var viewsToRemove = new List<CompiledViewDescriptor>();
            foreach (var descriptor in feature.ViewDescriptors) {
                if (IsIdentityUIView(descriptor)) {
                    switch (_framework) {
                        case UIFramework.Bootstrap5:
                            if (descriptor.Type?.FullName?.Contains(nameof(UIFramework.Tailwind), StringComparison.Ordinal) is true ||
                                descriptor.Type?.FullName?.Contains(nameof(UIFramework.Bootstrap4), StringComparison.Ordinal) is true) {
                                // Remove V5 views
                                viewsToRemove.Add(descriptor);
                            } else {
                                // Fix up paths to eliminate version subdir
                                descriptor.RelativePath = descriptor.RelativePath.Replace($"{nameof(UIFramework.Bootstrap5)}/", "");
                            }
                            break;
                        case UIFramework.Bootstrap4:
                            if (descriptor.Type?.FullName?.Contains(nameof(UIFramework.Tailwind), StringComparison.Ordinal) is true ||
                                descriptor.Type?.FullName?.Contains(nameof(UIFramework.Bootstrap5), StringComparison.Ordinal) is true) {
                                // Remove V5 views
                                viewsToRemove.Add(descriptor);
                            } else {
                                // Fix up paths to eliminate version subdir
                                descriptor.RelativePath = descriptor.RelativePath.Replace($"{nameof(UIFramework.Bootstrap4)}/", "");
                            }
                            break;
                        case UIFramework.Tailwind:
                            if (descriptor.Type?.FullName?.Contains(nameof(UIFramework.Bootstrap4), StringComparison.Ordinal) is true ||
                                descriptor.Type?.FullName?.Contains(nameof(UIFramework.Bootstrap5), StringComparison.Ordinal) is true) {
                                // Remove V4 views
                                viewsToRemove.Add(descriptor);
                            } else {
                                // Fix up paths to eliminate version subdir
                                descriptor.RelativePath = descriptor.RelativePath.Replace($"{nameof(UIFramework.Tailwind)}/", "");
                            }
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown framework: {_framework}");
                    }
                }
            }

            foreach (var descriptorToRemove in viewsToRemove) {
                feature.ViewDescriptors.Remove(descriptorToRemove);
            }
        }

        private static bool IsIdentityUIView(CompiledViewDescriptor desc) => (desc.RelativePath.StartsWith($"/Pages/{nameof(UIFramework.Bootstrap5)}", StringComparison.OrdinalIgnoreCase) ||
                                                                              desc.RelativePath.StartsWith($"/Pages/{nameof(UIFramework.Bootstrap4)}", StringComparison.OrdinalIgnoreCase) ||
                                                                              desc.RelativePath.StartsWith($"/Pages/{nameof(UIFramework.Tailwind)}", StringComparison.OrdinalIgnoreCase))
                                                                              &&
            desc.Type?.Assembly == typeof(IdentityBuilderUIExtensions).Assembly;
    }
}
