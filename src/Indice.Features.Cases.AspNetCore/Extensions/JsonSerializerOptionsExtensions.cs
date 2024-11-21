using System.Text.Json;

namespace Indice.Features.Cases.Extensions;

internal static class JsonSerializerOptionsExtensions
{
    public static JsonSerializerOptions WithIgnorePropertyNamingPolicy(this JsonSerializerOptions options) {
        options.PropertyNamingPolicy = null;
        return options;
    }
}