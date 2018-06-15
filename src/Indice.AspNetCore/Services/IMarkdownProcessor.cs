namespace Indice.Services
{
    /// <summary>
    /// A processing service that converts input markdown string to html string
    /// </summary>
    public interface IMarkdownProcessor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        string Convert(string markdown);
    }
}
