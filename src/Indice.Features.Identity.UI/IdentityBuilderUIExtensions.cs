using FluentValidation;
using FluentValidation.AspNetCore;
using Indice.Features.Identity.UI;
using Indice.Features.Identity.UI.Localization;
using Indice.Features.Identity.UI.Validators;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IServiceCollection"/> for registering required services for Identity UI pages feature.</summary>
public static class IdentityBuilderUIExtensions
{
    /// <summary>Adds the required services and pages for Identity UI pages features.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IServiceCollection AddIdentityUI(this IServiceCollection services, IConfiguration configuration) {
        // Post configure razor pages options.
        services.ConfigureOptions(typeof(IdentityUIConfigureOptions));
        // Configure FluentValidation.
        services.AddValidatorsFromAssemblyContaining<LoginInputModelValidator>();
        services.AddFluentValidationAutoValidation(config => {
            config.DisableDataAnnotationsValidation = true;
        });
        services.AddFluentValidationClientsideAdapters();
        // Configure required localization services.
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddMvcCore()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix, options => options.ResourcesPath = "Resources");
        services.AddTransient<IIdentityViewLocalizer, IdentityViewLocalizer>();
        // Configure other services.
        services.AddGeneralSettings(configuration);
        //services.AddClientAwareViewLocationExpander();
        services.AddMarkdown();
        return services;
    }
}
