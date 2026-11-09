using System.Text.Json;
using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using Indice.Features.Messages.Core.Rendering;
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

    [InlineData(
    "{\"sms\":{\"title\":\"incharge - Ολοκλήρωση Φόρτισης\",\"body\":\"Η φόρτιση στον σταθμο 50 €.\"},\"email\":{\"title\":\"Test email encoding\",\"body\":\"Hellooo\\n\\n&\\n\\nGoodbye\"}}",
    "sms: {{data.sms.body}}",
    "sms",
    "sms: Η φόρτιση στον σταθμο 50 €.")]

    [Theory]
    public static void TestHandlebarsTextEncoder(string data, string template, string channel, string expected) {
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

    private const string ConcatData = "{\"uiBaseUrl\":\"https://portal.example.gr?a=1&b=2\",\"propertyInfo\":{\"code\":\"PRP-001\",\"floor\":3,\"isNew\":true}}";

    [InlineData("{{concat data.uiBaseUrl \"/my-profile/buying/\" data.propertyInfo.code \"/documents\"}}", "https://portal.example.gr?a&#x3D;1&amp;b&#x3D;2/my-profile/buying/PRP-001/documents")]
    [InlineData("{{> text_link url=(concat data.uiBaseUrl \"/my-profile/buying/\" data.propertyInfo.code \"/documents\") text=\"Docs\"}}", "<a href=\"https://portal.example.gr?a&#x3D;1&amp;b&#x3D;2/my-profile/buying/PRP-001/documents\">Docs</a>")]
    [InlineData("{{> self_link path=\"/documents\"}}", "<a href=\"https://portal.example.gr?a&#x3D;1&amp;b&#x3D;2/documents\">PRP-001</a>")]
    [InlineData("[{{concat data.missing \"/x\" data.propertyInfo.missing}}]", "[/x]")]
    [InlineData("{{concat data.propertyInfo.floor \"-\" data.propertyInfo.isNew \"-\" 2.5}}", "3-true-2.5")]
    [InlineData("{{#if (concat data.missing)}}TRUE{{else}}FALSE{{/if}}", "FALSE")]
    [Theory]
    public void ConcatHelper(string template, string expected) {
        var handlebars = Handlebars.Create();
        handlebars.RegisterMessageHelpers();
        handlebars.Configuration.UseJson();
        handlebars.Configuration.TextEncoder = HandlebarsTextEncoderFactory.Create("Email");
        handlebars.RegisterTemplate("text_link", "<a href=\"{{url}}\">{{text}}</a>");
        handlebars.RegisterTemplate("self_link", "<a href=\"{{concat data.uiBaseUrl path}}\">{{data.propertyInfo.code}}</a>");
        dynamic templateData = new {
            data = JsonDocument.Parse(ConcatData)
        };
        var output = handlebars.Compile(template)(templateData);
        Assert.Equal(expected, output);
    }
}

