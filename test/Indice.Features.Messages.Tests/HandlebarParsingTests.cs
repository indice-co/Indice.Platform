using System.Text.Json;
using HandlebarsDotNet;
using Indice.Serialization;
using Xunit;

namespace Indice.Features.Messages.Tests;
public class HandlebarParsingTests
{
    const string Template = "This should repeat:\n{{#each data.items}}- {{title}}: {{href}} \n{{/each}}";

    [InlineData(@"{
    ""items"": [
      {
        ""title"": ""1"",
        ""href"": ""https://one.com""
      },
      {
        ""title"": ""2"",
        ""href"": ""https://two.com""
      }
    ]
  }", "This should repeat:\n- 1: https://one.com \n- 2: https://two.com \n")]
    [Theory]
    public void ParseInputDataToHandlebar(string input, string expected) {
        var handlebars = Handlebars.Create();
        handlebars.Configuration.TextEncoder = new HtmlEncoder();

        dynamic Data = input;
        dynamic templateData = new {
            title = "Welcome",
            data = Data is not null && (Data is not string || !string.IsNullOrWhiteSpace(Data))
                    ? JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonNode>(Data, JsonSerializerOptionDefaults.GetDefaultSettings())
                    : null
        };

        var output = handlebars.Compile(Template)(templateData);
       Assert.Equal(expected, output);
    }
}
