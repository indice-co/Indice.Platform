using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Cases.Core.Localization;

/// <summary>Shared resource service for localizing string to the consumer's module.</summary>
public class CaseSharedResourceService
{
    private readonly IStringLocalizer _localizer;

    /// <summary>Create the instance with the corresponding assembly name / location where the resx files exist.</summary>
    /// <param name="factory">The IStringLocalizerFactory factory</param>
    /// <param name="assemblyName">The assembly name where the .resx files exist.</param>
    public CaseSharedResourceService(IStringLocalizerFactory factory, string? assemblyName = null) {
        if (assemblyName == null) {
            var type = typeof(CaseSharedResource);
            assemblyName = type.Assembly.GetName().Name!;
        }
        _localizer = factory.Create(nameof(CaseSharedResource), assemblyName);
    }

    /// <summary>Get a localized string to the <see cref="CultureInfo.CurrentUICulture"/> locale.</summary>
    /// <param name="key">The key of the resource.</param>
    /// <param name="arguments">Any extra arguments.</param>
    public LocalizedString GetLocalizedHtmlString(string key, params object[] arguments) {
        return _localizer[key, arguments];
    }

    /// <summary>Get a localized string for a specific culture.</summary>
    /// <param name="key">The key of the resource.</param>
    /// <param name="culture">The specific culture to retrieve the localized string.</param>
    /// <param name="arguments">Any extra arguments.</param>
    public LocalizedString GetLocalizedHtmlStringWithCulture(string key, string culture, params object[] arguments) {
        // Store the current ui culture to a variable
        var currentCulture = CultureInfo.CurrentUICulture;
        
        // Change culture info according to culture parameter
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        
        // Get the localized string in the requested culture
        var result = _localizer[key, arguments];

        // Set the current ui culture to the original value
        CultureInfo.CurrentUICulture = new CultureInfo(currentCulture.Name);
        return result;
    }
}

/// <summary>Class to group shared case resources.</summary>
public class CaseSharedResource { }
