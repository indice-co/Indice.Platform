using System.Net.Mime;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Sanitizes <see cref="AIContent"/> parts by downgrading or re-typing unsafe media types (e.g. SVG/HTML)
/// so downstream renderers don't treat them as executable content.
/// </summary>
public static class ContentSanitizer
{
    /// <summary>
    /// Attempts to sanitize the <see cref="AIContent"/> parts in the list, modifying them in place. For example, it can remove potentially harmful content or format the text appropriately based on its media type.
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static IList<AIContent> SanitizeForLLMs(this IList<AIContent> parts) {
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
            } else if (part is FunctionCallContent || part is FunctionResultContent) {
                // skip form history because it may confuse the downstream llm since these are calls that surfaced from workflow and not the LLM.
                continue; 
            } else {
                sanitizedParts.Add(part);
            }
        }
        return sanitizedParts;
    }
    /// <summary>
    /// Attempts to sanitize the <see cref="AIContent"/> parts in the list, modifying them in place. For example, it can remove potentially harmful content or format the text appropriately based on its media type.
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static List<AIContent> SanitizeForUserHistory(this IList<AIContent> parts) {
        List<AIContent> sanitizedParts = new List<AIContent>();
        foreach (var part in parts) {
            if (part is FunctionCallContent || part is FunctionResultContent) {
                // skip form history because it may confuse the downstream llm since these are calls that surfaced from workflow and not the LLM.
                continue;
            } else {
                sanitizedParts.Add(part);
            }
        }
        return sanitizedParts;
    }

}
