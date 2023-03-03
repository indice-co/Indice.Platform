using System.Linq;
using System.Text.RegularExpressions;

namespace Indice.Extensions;

/// <summary>
/// Extensions that enable the creation of a unique alias given a suggested string name.
/// </summary>
public static class AliasExtensions
{
    /// <summary>
    /// Generates an alias. Alternate key from string.
    /// </summary>
    /// <param name="existingAliases">These are the already taken aliases</param>
    /// <param name="fallbackName">The name that wull be used to generate the alias (if not set) .</param>
    /// <param name="suggestedAlias">The alias.</param>
    /// <returns>The alias, after processing it.</returns>
    public static string DetermineAlias(this IQueryable<string> existingAliases, string fallbackName, string suggestedAlias) {
        // Check if user has selected an alias for the subscription.
        // If the alias is not set by the user, then we use the company name to generate one.
        if (string.IsNullOrEmpty(suggestedAlias)) {
            // There is no need to check for spaces or special characters in the company name as this is handled by the validation rules.
            var transliteratedName = fallbackName.Unidecode();
            var invalidCharPattern = @"[^0-9a-zA-Z ]+";
            var whitespacePattern = @"\s+";
            // We need to remove all special characters.
            suggestedAlias = Regex.Replace(transliteratedName, invalidCharPattern, string.Empty);
            // We need to remove whitespaces.
            suggestedAlias = Regex.Replace(suggestedAlias, whitespacePattern, "-").ToLowerInvariant().Trim('-');
        }
        var availability = CheckAvailability(existingAliases, suggestedAlias);
        if (!availability.IsAvailable) {
            var aliasParts = suggestedAlias.Split('-');
            var aliasLastIndex = availability.LastIndex;

            if (aliasLastIndex == -1) {
                aliasParts[aliasParts.Length - 1] = (aliasLastIndex + 1).ToString();
                suggestedAlias = string.Join("-", aliasParts);
            } else {
                suggestedAlias = $"{string.Join("-", aliasParts)}-{aliasLastIndex + 1}";
            }
        }
        return suggestedAlias;
    }

    /// <summary>
    /// Performs a call to the database to check if the company alias is available or not.
    /// </summary>
    /// <returns>Returns true if the alias is available, otherwise false.</returns>
    private static AliasAvailability CheckAvailability(IQueryable<string> existingAliases, string newAlias) {
        var lastIndex = 0;
        var similarAliases = existingAliases.Where(x => x.StartsWith(newAlias)).ToList();
        var isAvailable = !similarAliases.Any();
        if (isAvailable) {
            // If the alias is available then the alias index will not be used.
            lastIndex = -1;
        } else {
            var indexes = similarAliases.Select(x => {
                if (int.TryParse(x.Split('-').Last(), out var aliasIndex)) {
                    return aliasIndex;
                }

                return 0;
            })
            .OrderBy(x => x)
            .ToList();
            // If this is the case then we return that index, otherwise 0. We want company aliases to be like 'my-corp, my-corp-1, my-corp-2' etc.
            lastIndex = indexes.Count > 0 ? indexes.Last() : 0;
        }
        var availabeAlias = new AliasAvailability {
            IsAvailable = isAvailable,
            LastIndex = lastIndex
        };
        return availabeAlias;
    }

    /// <summary>
    /// Small helper class used to report for alias availability
    /// </summary>
    public class AliasAvailability
    {
        /// <summary>
        /// The suggested alias is available
        /// </summary>
        public bool IsAvailable { get; set; }
        /// <summary>
        /// The last index used for this alias.
        /// </summary>
        public int LastIndex { get; set; }
    }
}
