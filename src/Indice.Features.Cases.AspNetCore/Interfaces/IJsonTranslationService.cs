namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// Json Translation Interface
    /// </summary>
    public interface IJsonTranslationService
    {
        /// <summary>
        /// Translate JSON property values from a given JSON translations object.
        /// </summary>
        /// <param name="jsonSource">The JSON source. Can be anything as long as it's a valid JSON.</param>
        /// <param name="jsonTranslations">The translations in a JSON object. Must be deserialized as <see cref="Dictionary{string,string}"/>.</param>
        /// <param name="language">The language to translate into as TwoLetterISOLanguageName.</param>
        /// <returns>The JSON with the translated values as string.</returns>
        string Translate(string jsonSource, string jsonTranslations, string language);
    }
}