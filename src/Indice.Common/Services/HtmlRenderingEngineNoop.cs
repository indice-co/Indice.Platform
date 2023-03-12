namespace Indice.Services;

/// <summary>A default implementation for <see cref="IHtmlRenderingEngine"/> that does nothing. Passes through the given template to the output as if it was the body of the HTML.</summary>
public class HtmlRenderingEngineNoop : IHtmlRenderingEngine
{
    /// <inheritdoc/>
    public Task<string> RenderAsync(string template, Type dataType, object data) => Task.FromResult(template);
}
