using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Humanizer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Indice.AspNetCore.ModelBinding;

/// <summary>Using the property name and humanizer breaks the prop name into a human readable form and updates the display metadata for that field.</summary>
public class HumanizerMetadataProvider : IDisplayMetadataProvider
{
    /// <inheritdoc />
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context) {
        var propertyAttributes = context.Attributes;
        var modelMetadata = context.DisplayMetadata;
        var propertyName = context.Key.Name;
        if (IsTransformRequired(propertyName, modelMetadata, propertyAttributes)) {
            modelMetadata.DisplayName = () => propertyName.Humanize().Transform(To.TitleCase);
        }
    }

    private static bool IsTransformRequired(string propertyName, DisplayMetadata modelMetadata, IReadOnlyList<object> propertyAttributes) {
        if (!string.IsNullOrEmpty(modelMetadata.SimpleDisplayProperty)) {
            return false;
        }
        if (propertyAttributes.OfType<DisplayNameAttribute>().Any()) {
            return false;
        }
        if (propertyAttributes.OfType<DisplayAttribute>().Any()) {
            return false;
        }
        if (string.IsNullOrEmpty(propertyName)) {
            return false;
        }
        return true;
    }
}
