using System.Net.Mime;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// 
/// </summary>
public static class ContentSanitizer
{
    /// <summary>
    /// Attempts to sanitize the <see cref="AIContent"/> parts in the list, modifying them in place. For example, it can remove potentially harmful content or format the text appropriately based on its media type.
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static IList<AIContent> Sanitize(this IList<AIContent> parts) {
        IList<AIContent> sanitizedParts = new List<AIContent>();
        foreach (var part in parts) {
            if (part is DataContent dataContent) {
                switch(dataContent.MediaType) {
                    case MediaTypeNames.Image.Svg:
                        sanitizedParts.Add(new DataContent(dataContent.Data,AgentsConstants.MediaTypes.Svg) { Name = dataContent.Name});
                        break;
                    case MediaTypeNames.Text.Html:
                        var textContent = new DataContent(dataContent.Data,AgentsConstants.MediaTypes.Html) { Name = dataContent.Name };
                        sanitizedParts.Add(textContent);
                        break;
                    default:
                        sanitizedParts.Add(dataContent);
                        break;
                }
            }
            else {
                sanitizedParts.Add(part);
            }
        }
        return sanitizedParts;
    }

}
