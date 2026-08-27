using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Tests;

public class AIContentTests
{
    [Fact]
    public async Task DataContentTest() {
        // example Data URI: data:image/png;base64,iVBORw0KGgo...
        var plainText = "Hello, world!";
        var jsonText = "{\"test\": \"this is value\"}";
        var svgText = """
           <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#04AA6D" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M20 6L9 17l-5-5"/>
            </svg>
           """;
        var htmlText = """
           <html>
              <body>
                 <h1>Hello, world!</h1>
              </body>
           </html>
           """;
        var dataContentPlainText = new DataContent($"data:,{Uri.EscapeDataString(plainText)}");
        var dataContentJson = new DataContent($"data:,{Uri.EscapeDataString(jsonText)}", MediaTypeNames.Application.Json);
        var dataContentImage = await DataContent.LoadFromAsync(Path.Join(Directory.GetCurrentDirectory(), "favicon-16.png"), cancellationToken: TestContext.Current.CancellationToken);
        var dataContentSvg = new DataContent($"data:,{Uri.EscapeDataString(svgText)}", MediaTypeNames.Image.Svg);
        var dataContentHtml = new DataContent($"data:,{Uri.EscapeDataString(htmlText)}", MediaTypeNames.Text.Html);
        Assert.True(Encoding.UTF8.GetBytes(plainText).SequenceEqual(dataContentPlainText.Data.ToArray()));
        Assert.Equal(jsonText, Encoding.UTF8.GetString(dataContentJson.Data.ToArray()));
        Assert.Equal(MediaTypeNames.Application.Json, dataContentJson.MediaType);
        Assert.Equal(MediaTypeNames.Image.Png, dataContentImage.MediaType);
        Assert.True(Encoding.UTF8.GetBytes(svgText).SequenceEqual(dataContentSvg.Data.ToArray()));
        Assert.Equal(MediaTypeNames.Image.Svg, dataContentSvg.MediaType);
        Assert.True(Encoding.UTF8.GetBytes(htmlText).SequenceEqual(dataContentHtml.Data.ToArray()));
        Assert.Equal(htmlText, Encoding.UTF8.GetString(dataContentHtml.Data.ToArray()));
        Assert.Equal(MediaTypeNames.Text.Html, dataContentHtml.MediaType);
    }
}
