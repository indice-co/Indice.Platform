namespace Indice.Services;

/// <summary>
/// This is an abstraction for the rendering engine. Usually works with <see cref="IEmailService"/> implementation. 
/// Decouple the email sending from the content rendering.
/// </summary>
public interface IHtmlRenderingEngine {
    /// <summary>Renders the HTML given a template and an object representing data.</summary>
    /// <param name="template">Can be either a template name or the body of the template according to implementation.</param>
    /// <param name="dataType">The data type of <paramref name="data"/>.</param>
    /// <param name="data">The data usually used to substitute/bind to the given <paramref name="template"/>.</param>
    /// <returns>The rendered HTML output.</returns>
    Task<string> RenderAsync(string template, Type dataType, object data);
}

/// <summary>Extensions on <see cref="IHtmlRenderingEngine"/>.</summary>
public static class HtmlRenderingEngineExtensions
{
    /// <summary>Renders the HTML given a template and an object representing data.</summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <param name="engine">The rendering engine.</param>
    /// <param name="template">Can be either a template name or the body of the template according to implementation.</param>
    /// <param name="data">The data usually used to substitute/bind to the given <paramref name="template"/>.</param>
    /// <returns>The rendered HTML output.</returns>
    public static Task<string> RenderAsync<TData>(this IHtmlRenderingEngine engine, string template, TData data) =>
        engine.RenderAsync(template, data!.GetType(), data);

}
