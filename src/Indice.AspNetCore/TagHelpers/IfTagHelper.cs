using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Indice.AspNetCore.TagHelpers
{
    /// <summary>
    /// Suppresses the output of the element if the supplied predicate equates to <c>false</c>.
    /// </summary>
    [HtmlTargetElement("*", Attributes = "indice-if")]
    public class IfTagHelper : TagHelper
    {
        /// <summary>
        /// The predicate expression to test.
        /// </summary>
        [HtmlAttributeName("indice-if")]
        public bool Predicate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="output"></param>
        public override void Process(TagHelperContext context, TagHelperOutput output) {
            if (!Predicate) {
                output.SuppressOutput();
            }
        }
    }
}
