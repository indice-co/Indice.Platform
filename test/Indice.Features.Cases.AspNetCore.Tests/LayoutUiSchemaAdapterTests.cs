using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services;

namespace Indice.Features.Cases.Tests;

[Trait("CaseManagement", "LayoutUiSchemaAdapter")]
public class LayoutUiSchemaAdapterTests
{
    // Same layout as JsonTranslationServiceTest: sections, flex rows, conditions, file widgets, validation messages and an html block.
    private const string PhonesLayout = """[{"type":"section","items":[{"key":"homePhoneSelected","title":"Θέλω να επικαιροποιήσω το Σταθερό Τηλέφωνο μου"}]},{"type":"section","title":"Σταθερό τηλέφωνο","labelHtmlClass":"px-2","condition":"homePhoneSelected","items":[{"type":"flex","flex-flow":"row wrap","items":[{"key":"homePhone.countryCode","flex":"1 1 280px","title":"Κωδικός χώρας","validationMessages":{"required":"Υποχρεωτικό Πεδίο."}},{"key":"homePhone.number","flex":"1 1 280px","title":"Σταθερό τηλέφωνο","placeholder":"Σταθερό τηλέφωνο","validationMessages":{"required":"Υποχρεωτικό Πεδίο.","pattern":"Λανθασμένη μορφή τηλεφώνου"}}]},{"type":"flex","flex-flow":"row wrap","items":[{"key":"homePhone.documentType","flex":"1 1 280px","title":"Είδος εγγράφου","validationMessages":{"required":"Υποχρεωτικό Πεδίο."}},{"key":"homePhone.attachmentId","title":"Έγγραφο σταθερού τηλεφώνου","type":"file","flex":"1 1 280px","validationMessages":{"required":"Υποχρεωτικό Πεδίο."}}]},{"key":"homePhone.requiresAlternativeDocument","htmlClass":"w-50","title":"Είναι το έγγραφο σε άλλο όνομα;"},{"condition":"homePhone.requiresAlternativeDocument=='YES'","type":"flex","flex-flow":"row wrap","items":[{"key":"homePhone.alternativeDocumentType","flex":"1 1 280px","title":"Είδος συμπληρωματικού εγγράφου"},{"key":"homePhone.alternativeAttachmentId","flex":"1 1 280px","type":"file","title":"Συμπληρωματικό έγγραφο σταθερού τηλεφώνου"}]}]},{"type":"html","helpvalue":"<hr /><span>(*) Υποχρεωτικό πεδίο</span>"}]""";

    [Fact]
    public void Convert_NullLayout_ReturnsNullWithoutWarnings() {
        var result = LayoutUiSchemaAdapter.Convert(null);

        Assert.Null(result.UiSchema);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Convert_RealLayout_ProducesExpectedUiSchemaWithoutWarnings() {
        var result = LayoutUiSchemaAdapter.Convert(JsonNode.Parse(PhonesLayout));

        Assert.Empty(result.Warnings);
        var root = result.UiSchema!.AsObject();
        Assert.Equal("VerticalLayout", (string?)root["type"]);
        var elements = root["elements"]!.AsArray();
        Assert.Equal(3, elements.Count);

        // Section without a title -> vertical layout holding the checkbox control.
        Assert.Equal("VerticalLayout", (string?)elements[0]!["type"]);
        var checkbox = elements[0]!["elements"]![0]!;
        Assert.Equal("Control", (string?)checkbox["type"]);
        Assert.Equal("#/properties/homePhoneSelected", (string?)checkbox["scope"]);
        Assert.Equal("Θέλω να επικαιροποιήσω το Σταθερό Τηλέφωνο μου", (string?)checkbox["label"]);

        // Section with a title -> group; condition and css classes are kept in options for the client renderer.
        var group = elements[1]!;
        Assert.Equal("Group", (string?)group["type"]);
        Assert.Equal("Σταθερό τηλέφωνο", (string?)group["label"]);
        Assert.Equal("homePhoneSelected", (string?)group["options"]!["condition"]);
        Assert.Equal("px-2", (string?)group["options"]!["labelHtmlClass"]);

        // Flex row -> horizontal layout; control options keep flex sizing, placeholder and validation messages.
        var row = group["elements"]![0]!;
        Assert.Equal("HorizontalLayout", (string?)row["type"]);
        Assert.Equal("row wrap", (string?)row["options"]!["flex-flow"]);
        var number = row["elements"]![1]!;
        Assert.Equal("#/properties/homePhone/properties/number", (string?)number["scope"]);
        Assert.Equal("1 1 280px", (string?)number["options"]!["flex"]);
        Assert.Equal("Σταθερό τηλέφωνο", (string?)number["options"]!["placeholder"]);
        Assert.Equal("Λανθασμένη μορφή τηλεφώνου", (string?)number["options"]!["validationMessages"]!["pattern"]);

        // ajsf widget type -> options.widget.
        var attachment = group["elements"]![1]!["elements"]![1]!;
        Assert.Equal("#/properties/homePhone/properties/attachmentId", (string?)attachment["scope"]);
        Assert.Equal("file", (string?)attachment["options"]!["widget"]);

        // Conditional flex row: condition expression copied unchanged.
        var conditionalRow = group["elements"]![3]!;
        Assert.Equal("homePhone.requiresAlternativeDocument=='YES'", (string?)conditionalRow["options"]!["condition"]);

        // Html block -> custom Html element with the markup in options.
        Assert.Equal("Html", (string?)elements[2]!["type"]);
        Assert.Equal("<hr /><span>(*) Υποχρεωτικό πεδίο</span>", (string?)elements[2]!["options"]!["helpvalue"]);
    }

    [Fact]
    public void Convert_LayoutPerSchemaSelector_ConvertsEachSelector() {
        var layout = JsonNode.Parse("""{"frontend":[{"key":"name"}],"backoffice":[{"key":"name","readonly":true}]}""");

        var result = LayoutUiSchemaAdapter.Convert(layout);

        Assert.Empty(result.Warnings);
        Assert.Equal("VerticalLayout", (string?)result.UiSchema!["frontend"]!["type"]);
        Assert.Equal(true, (bool?)result.UiSchema!["backoffice"]!["elements"]![0]!["options"]!["readonly"]);
    }

    [Fact]
    public void Convert_LayoutAlreadyUiSchema_ReturnsItUnchanged() {
        var layout = JsonNode.Parse("""{"type":"VerticalLayout","elements":[{"type":"Control","scope":"#/properties/name"}]}""");

        var result = LayoutUiSchemaAdapter.Convert(layout);

        Assert.Empty(result.Warnings);
        Assert.True(JsonNode.DeepEquals(layout, result.UiSchema));
    }

    [Fact]
    public void Convert_Keys_MapToScopes() {
        var layout = JsonNode.Parse("""["name","address.street","/address/city","phones[].number"]""");

        var scopes = LayoutUiSchemaAdapter.Convert(layout).UiSchema!["elements"]!.AsArray().Select(e => (string?)e!["scope"]);

        Assert.Equal(["#/properties/name", "#/properties/address/properties/street", "#/properties/address/properties/city", "#/properties/phones/items/properties/number"], scopes);
    }

    [Fact]
    public void Convert_ArrayWithItemLayout_UsesDetailScopedToTheItem() {
        var layout = JsonNode.Parse("""[{"key":"phones","type":"array","items":["phones[].number",{"key":"phones[].type","type":"select"}]}]""");

        var control = LayoutUiSchemaAdapter.Convert(layout).UiSchema!["elements"]![0]!;

        Assert.Equal("#/properties/phones", (string?)control["scope"]);
        Assert.Null(control["options"]!["widget"]);
        var detail = control["options"]!["detail"]!["elements"]!.AsArray();
        Assert.Equal("#/properties/number", (string?)detail[0]!["scope"]);
        Assert.Equal("#/properties/type", (string?)detail[1]!["scope"]);
        Assert.Equal("select", (string?)detail[1]!["options"]!["widget"]);
    }

    [Fact]
    public void Convert_Tabs_ProducesCategorization() {
        var layout = JsonNode.Parse("""[{"type":"tabs","tabs":[{"title":"Personal","items":["name"]},{"title":"Address","items":["address.street"]}]}]""");

        var categorization = LayoutUiSchemaAdapter.Convert(layout).UiSchema!["elements"]![0]!;

        Assert.Equal("Categorization", (string?)categorization["type"]);
        Assert.Equal("Category", (string?)categorization["elements"]![0]!["type"]);
        Assert.Equal("Address", (string?)categorization["elements"]![1]!["label"]);
        Assert.Equal("#/properties/address/properties/street", (string?)categorization["elements"]![1]!["elements"]![0]!["scope"]);
    }

    [Fact]
    public void Convert_NoTitle_HidesLabel() {
        var layout = JsonNode.Parse("""[{"key":"name","title":"Name","notitle":true}]""");

        var control = LayoutUiSchemaAdapter.Convert(layout).UiSchema!["elements"]![0]!;

        Assert.Equal(false, (bool?)control["label"]);
    }

    [Theory]
    [InlineData("""{"type":"flex","flex-flow":"row wrap","items":["name"]}""", "HorizontalLayout")]
    [InlineData("""{"type":"flex","flex-flow":"column","items":["name"]}""", "VerticalLayout")]
    [InlineData("""{"type":"flex","items":["name"]}""", "VerticalLayout")]
    public void Convert_Flex_UsesTheFlexDirection(string flex, string expectedType) {
        var layout = new JsonArray(JsonNode.Parse(flex));

        var element = LayoutUiSchemaAdapter.Convert(layout).UiSchema!["elements"]![0]!;

        Assert.Equal(expectedType, (string?)element["type"]);
    }

    [Fact]
    public void Convert_LabelOnlyWithoutKey_ProducesLabelOnlyElement() {
        var layout = JsonNode.Parse("""[{"title":"Update","type":"label-only","extraType":"data-bind href","data":"{baseAddress}/api/update/{id}","linkText":"Update"}]""");

        var result = LayoutUiSchemaAdapter.Convert(layout);

        Assert.Empty(result.Warnings);
        var element = result.UiSchema!["elements"]![0]!;
        Assert.Equal("LabelOnly", (string?)element["type"]);
        Assert.Equal("Update", (string?)element["label"]);
        Assert.Equal("{baseAddress}/api/update/{id}", (string?)element["options"]!["data"]);
    }

    [Fact]
    public void Convert_ScriptsAreCopiedToOptions() {
        var layout = JsonNode.Parse("""[{"key":"amount","onChange":"(value, model) => {}","onInit":"(value, model, metadata) => {}"}]""");

        var options = LayoutUiSchemaAdapter.Convert(layout).UiSchema!["elements"]![0]!["options"]!;

        Assert.Equal("(value, model) => {}", (string?)options["onChange"]);
        Assert.Equal("(value, model, metadata) => {}", (string?)options["onInit"]);
    }

    [Fact]
    public void Convert_UnsupportedItems_AreSkippedWithWarnings() {
        var layout = JsonNode.Parse("""["*",{"type":"unknown-widget"},{"type":"carousel","items":["name"]}]""");

        var result = LayoutUiSchemaAdapter.Convert(layout);

        Assert.Equal(3, result.Warnings.Count);
        Assert.Contains(result.Warnings, w => w.StartsWith("$[0]:"));
        Assert.Contains(result.Warnings, w => w.StartsWith("$[1]:"));
        Assert.Contains(result.Warnings, w => w.StartsWith("$[2]:") && w.Contains("carousel"));
        // The unknown container is still converted, so its fields are not lost.
        Assert.Equal("#/properties/name", (string?)result.UiSchema!["elements"]![0]!["elements"]![0]!["scope"]);
    }

    [Theory]
    [InlineData("""{"$schema":"http://json-schema.org/draft-07/schema#","type":"object"}""", false)]
    [InlineData("""{"type":"object"}""", false)]
    [InlineData("""{"$schema":"http://json-schema.org/draft-04/schema#","type":"object"}""", true)]
    [InlineData("""{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"object"}""", true)]
    public void Convert_SchemaDraft_WarnsWhenNotDraft07(string schema, bool expectWarning) {
        var result = LayoutUiSchemaAdapter.Convert(JsonNode.Parse("[]"), JsonNode.Parse(schema));

        Assert.Equal(expectWarning, result.Warnings.Any(w => w.StartsWith("$schema:")));
    }

    [Fact]
    public void ResponseModels_ExposeUiSchemaGeneratedFromLayout() {
        var layout = JsonNode.Parse("""["name"]""");

        var caseType = new CaseType { Layout = layout };
        var caseTypePartial = new CaseTypePartial { Layout = layout };

        Assert.Equal("#/properties/name", (string?)caseType.UiSchema!["elements"]![0]!["scope"]);
        Assert.Equal("#/properties/name", (string?)caseTypePartial.UiSchema!["elements"]![0]!["scope"]);
        Assert.Null(new CaseType().UiSchema);
    }
}
