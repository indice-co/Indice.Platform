using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>
    /// This is an abstraction for the rendering Engine. Usualy works with <see cref="IEmailService"/> implementation. 
    /// Decouple the email sending from the content rendering. 
    /// </summary>
    public interface IHtmlRenderingEngine {
        /// <summary>
        /// Renders the html given a template and an object representing data
        /// </summary>
        /// <param name="template">can be either a template name or the body of the template according to implementation</param>
        /// <param name="dataType">The data type of <paramref name="data"/></param>
        /// <param name="data">The data usualy used to substitute/bind to the given <paramref name="template"/></param>
        /// <returns>The rendered html output.</returns>
        Task<string> RenderAsync(string template, Type dataType, object data);
    }

    /// <summary>
    /// Extensions on <see cref="IHtmlRenderingEngine"/>
    /// </summary>
    public static class HtmlRenderingEngineExtensions
    {
        /// <summary>
        /// Renders the html given a template and an object representing data
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="engine">The engine</param>
        /// <param name="template">can be either a template name or the body of the template according to implementation</param>
        /// <param name="data">The data usualy used to substitute/bind to the given <paramref name="template"/></param>
        /// <returns>The rendered html output.</returns>
        public static Task<string> RenderAsyncRenderAsync<TData>(this IHtmlRenderingEngine engine, string template, TData data) =>
            engine.RenderAsync(template, typeof(TData), data);

    }

    /// <summary>
    /// A default implementation for <see cref="IHtmlRenderingEngine"/> that does nothing. Passes through the given template to the output as if it was the body of the html.
    /// </summary>
    public class HtmlRenderingEngineNoOp : IHtmlRenderingEngine
    {
        /// <inheritdoc/>
        public Task<string> RenderAsync(string template, Type dataType, object data) => Task.FromResult(template);
    }
}
