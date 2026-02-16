using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Rendering;
using Indice.Serialization;
using Indice.Types;
using Xunit;

namespace Indice.Features.Messages.Tests;
public class HandlebarParsingTests
{
    [InlineData("{\"Description\": \"IsSuccess value is false, output should be FALSE\",\"IsSuccess\": false}", "{{#if data.IsSuccess }}TRUE{{else}}FALSE{{/if}}", "FALSE")]
    [InlineData("{\"Description\": \"IsSuccess value is true, output should be TRUE\",\"IsSuccess\": true}", "{{#if data.IsSuccess }}TRUE{{else}}FALSE{{/if}}", "TRUE")]
    [InlineData("{\"Description\": \"IsSuccess value is missing, output should be FALSE\",\"NotSuccess\": true}", "{{#if data.IsSuccess }}TRUE{{else}}FALSE{{/if}}", "FALSE")]
    [InlineData("{\"Description\": \"IsSuccess value is not bool, output should be TRUE\",\"IsSuccess\": \"otinanane\"}", "{{#if data.IsSuccess }}TRUE{{else}}FALSE{{/if}}", "TRUE")]
    [InlineData("{\"Innner\":{\"Description\": \"IsSuccess value is false, output should be FALSE\",\"IsSuccess\": false}}", "{{#if data.Innner.IsSuccess }}TRUE{{else}}FALSE{{/if}}", "FALSE")]
    [InlineData("{\"Innner\":{\"Description\": \"IsSuccess value is true, output should be TRUE\",\"IsSuccess\": true}}", "{{#if data.Innner.IsSuccess }}TRUE{{else}}FALSE{{/if}}", "TRUE")]
    [InlineData(
        "{\"customData\":{\"mitosStatus\":{\"Abandoned\":true}}}",
        "{{#if data.customData.mitosStatus.Abandoned}}Abandoned{{/if}}{{#if data.customData.mitosStatus.Approved}}Approved{{/if}}{{#if data.customData.mitosStatus.Disbursement}}Disbursement{{/if}}",
        "Abandoned")]
    [InlineData(@"{""items"": [
      {""title"": ""1"", ""href"": ""https://one.com""},
      {""title"": ""2"",""href"": ""https://two.com""}
    ]}", "This should repeat:\n{{#each data.items}}- {{title}}: {{href}} \n{{/each}}", "This should repeat:\n- 1: https://one.com \n- 2: https://two.com \n")]
    [Theory]
    public void ParseInputDataToHandlebar(string data, string template, string expected) {
        var handlebars = Handlebars.Create();
        handlebars.Configuration.TextEncoder = new HtmlEncoder();
        handlebars.Configuration.UseJson();
        dynamic templateData = new {
            title = "Welcome",
            data = data is not null && (data is not string || !string.IsNullOrWhiteSpace(data))
                    ? JsonDocument.Parse(data)
                    : null
        };
        var output = handlebars.Compile(template)(templateData);
        Assert.Equal(expected, output);
    }


    [InlineData(
    "{\"inbox\":{\"title\":\"Test sms encoding\",\"body\":\"Placeholder for real value\"},\"sms\":{\"title\":\"Test sms encoding\",\"body\":\"Hellooo\\n\\n&\\n\\nGoodbye\"}}",
    "SMS: {{data.sms.body}}",
    "SMS",
    "SMS: Hellooo\n\n&\n\nGoodbye")]

    [InlineData(
    "{\"inbox\":{\"title\":\"Test sms encoding\",\"body\":\"Placeholder for real value\"},\"sms\":{\"title\":\"Test sms encoding\",\"body\":\"Hellooo\\n\\n&\\n\\nGoodbye\"},\"email\":{\"title\":\"Test email encoding\",\"body\":\"Hellooo\\n\\n&\\n\\nGoodbye\"}}",
    "Email: {{data.email.body}}",
    "Email",
    "Email: Hellooo\n\n&amp;\n\nGoodbye")]

    [Theory]
    private static void TestHandlebarsTextEncoder(string data, string template, string channel, string expected) {
        var handlebars = Handlebars.Create();
        handlebars.Configuration.UseJson();
        handlebars.Configuration.TextEncoder = HandlebarsTextEncoderFactory.Create(channel);
        dynamic templateData = new {
            title = "Welcome",
            data = data is not null && (data is not string || !string.IsNullOrWhiteSpace(data))
                    ? JsonDocument.Parse(data)
                    : null
        };
        var output = handlebars.Compile(template)(templateData);
        Assert.Equal(expected, output);
    }
}

