using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Cases.Tests;

[Trait("CaseManagement", "LayoutTranslationsServiceTests")]
public class JsonTranslationServiceTest
{
    private readonly IJsonTranslationService _service;

    public JsonTranslationServiceTest() {
        var configurationMock = new Mock<IConfiguration>();
        configurationMock
            .Setup(p => p.GetSection("PrimaryTranslationLanguage").Value)
            .Returns(() => "el");

        _service = new JsonTranslationService(configurationMock.Object);
    }

    private static string SourceJson = """[{"type":"section","items":[{"key":"homePhoneSelected","title":"Θέλω να επικαιροποιήσω το Σταθερό Τηλέφωνο μου"}]},{"type":"section","title":"Σταθερό τηλέφωνο","labelHtmlClass":"px-2","condition":"homePhoneSelected","items":[{"type":"flex","flex-flow":"row wrap","items":[{"key":"homePhone.countryCode","flex":"1 1 280px","title":"Κωδικός χώρας","validationMessages":{"required":"Υποχρεωτικό Πεδίο."}},{"key":"homePhone.number","flex":"1 1 280px","title":"Σταθερό τηλέφωνο","placeholder":"Σταθερό τηλέφωνο","validationMessages":{"required":"Υποχρεωτικό Πεδίο.","pattern":"Λανθασμένη μορφή τηλεφώνου"}}]},{"type":"flex","flex-flow":"row wrap","items":[{"key":"homePhone.documentType","flex":"1 1 280px","title":"Είδος εγγράφου","validationMessages":{"required":"Υποχρεωτικό Πεδίο."}},{"key":"homePhone.attachmentId","title":"Έγγραφο σταθερού τηλεφώνου","type":"file","flex":"1 1 280px","validationMessages":{"required":"Υποχρεωτικό Πεδίο."}}]},{"key":"homePhone.requiresAlternativeDocument","htmlClass":"w-50","title":"Είναι το έγγραφο σε άλλο όνομα;"},{"condition":"homePhone.requiresAlternativeDocument=='YES'","type":"flex","flex-flow":"row wrap","items":[{"key":"homePhone.alternativeDocumentType","flex":"1 1 280px","title":"Είδος συμπληρωματικού εγγράφου"},{"key":"homePhone.alternativeAttachmentId","flex":"1 1 280px","type":"file","title":"Συμπληρωματικό έγγραφο σταθερού τηλεφώνου"}]}]},{"type":"section","items":[{"key":"mobilePhoneSelected","title":"Θέλω να επικαιροποιήσω το Κινητό Τηλέφωνο μου"}]},{"type":"section","title":"Κινητό τηλέφωνο","condition":"mobilePhoneSelected","items":[{"type":"flex","flex-flow":"row wrap","items":[{"key":"mobilePhone.countryCode","flex":"1 1 280px","title":"Κωδικός χώρας","validationMessages":{"required":"Υποχρεωτικό Πεδίο."}},{"key":"mobilePhone.number","flex":"1 1 280px","title":"Κινητό τηλέφωνο","placeholder":"Κινητό τηλέφωνο","validationMessages":{"required":"Υποχρεωτικό Πεδίο.","pattern":"Λανθασμένη μορφή τηλεφώνου"}}]},{"type":"flex","flex-flow":"row wrap","items":[{"key":"mobilePhone.documentType","flex":"1 1 280px","title":"Είδος εγγράφου","validationMessages":{"required":"Υποχρεωτικό Πεδίο."}},{"key":"mobilePhone.attachmentId","title":"Έγγραφο κινητού τηλεφώνου","type":"file","flex":"1 1 280px","validationMessages":{"required":"Υποχρεωτικό Πεδίο."}}]},{"key":"mobilePhone.requiresAlternativeDocument","htmlClass":"w-50","title":"Είναι το έγγραφο σε άλλο όνομα;"},{"condition":"mobilePhone.requiresAlternativeDocument=='YES'","type":"flex","flex-flow":"row wrap","items":[{"key":"mobilePhone.alternativeDocumentType","flex":"1 1 280px","title":"Είδος συμπληρωματικού εγγράφου"},{"key":"mobilePhone.alternativeAttachmentId","flex":"1 1 280px","type":"file","title":"Συμπληρωματικό έγγραφο κινητού τηλεφώνου"}]}]},{"type":"html","helpvalue":"<hr /><div class='row'><div class='col-12 text-evergreen text-10 mb-2'><span>(*) Υποχρεωτικό πεδίο</span></div></div><div class='row'><div class='col-lg-12 mt-2'><div class='card col-md-12 disclaimer-card'><div class='card-body'><div class='text-evergreen text-20 html-section'><ul><li>Οι λογαριασμοί τηλεφωνίας θα πρέπει να είναι πρόσφατοι (τελευταίων 3 μηνών).</li><li>Υ.Δ. τηλεφώνου κατοικίας: Περί χρήσης του αρ. τηλεφώνου από εσάς (υπογεγραμμένη από το πρόσωπο στου οποίου το όνομα εκδίδεται ο λογαριασμός).</li><li>Υ.Δ. Οικογενειακά προγράμματα- Περί χρήσης του αρ. τηλεφώνου από εσάς (υπογεγραμμένη από το πρόσωπο στου οποίου το όνομα εκδίδεται ο λογαριασμός).</li><li>Bεβαίωση εργοδότη - Εταιρικά προγράμματα: Περί χρήσης του αρ. τηλεφώνου από εσάς.</li><li>Οι υπέυθυνες δηλώσεις μπορούν να εκδοθούν ηλεκτρονικά μέσω της ψηφιακής πύλης του Δημοσίου gov.gr από τον ακόλουθο σύνδεσμο: <a target=\"_blank\" class=\"text-primary\" href=\"https://www.gov.gr/ipiresies/polites-kai-kathemerinoteta/psephiaka-eggrapha-gov-gr/ekdose-upeuthunes-deloses\">Έκδοση Υπεύθυνης Δήλωσης</a>. Υπεύθυνη δήλωση, η οποία δεν έχει εκδοθεί ηλεκτρονικά, θα πρέπει να φέρει γνήσιο υπογραφής από Κ.Ε.Π.</li></ul></div></div></div></div></div>"}]""";
    private static string Translations = """{"Θέλω να επικαιροποιήσω το Σταθερό Τηλέφωνο μου":"I want to validate my home phone","Σταθερό τηλέφωνο":"Home Phone","Κωδικός χώρας":"Country code","Υποχρεωτικό Πεδίο.":"required"}""";

    [Fact]
    public void Translate_EmptyLayout_ReturnSourceJson() {
        var translations = _service.Translate(null, [], "el");
        Assert.Null(translations);
    }

    [Fact]
    public void Translate_EmptyTranslations_ReturnSourceJson() {
        var translations = _service.Translate(JsonNode.Parse("""{"source": ""}"""), null, "el");
        Assert.Equal("""{"source":""}""", translations.ToJsonString());
    }

    [Fact]
    public void Translate_EmptyLocale_Throws() {
        Assert.Throws<ArgumentException>(() => _service.Translate(JsonNode.Parse("""{"source": ""}"""), [], string.Empty));
    }

    [Fact]
    public void Translate_StringValue_ValueNotChanged() {
        var layout = """[{"section":"test"}]""";
        var translations = _service.Translate(layout, JsonSerializer.Deserialize<Dictionary<string,string>>(Translations), "en");
        Assert.Equal("""[{"section":"test"}]""", translations);
    }

    [Fact]
    public void Translate_IntegerValue_ValueNotChanged() {
        var layout = """[{"section":"test","someNumber":32}]""";
        var translations = _service.Translate(layout, JsonSerializer.Deserialize<Dictionary<string, string>>(Translations), "en");
        Assert.Equal("""[{"section":"test","someNumber":32}]""", translations.ToJsonString());
    }

    [Fact]
    public void Translate_DecimalValue_ValueNotChanged() {
        var layout = """[{"section":"test","someNumber":32.31}]""";
        var translations = _service.Translate(layout, JsonSerializer.Deserialize<Dictionary<string, string>>(Translations), "en");
        Assert.Equal("""[{"section":"test","someNumber":32.31}]""", translations.ToJsonString());
    }

    [Fact]
    public void Translate_DateTimeValue_ValueNotChanged() {
        var date = DateTime.UtcNow;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        var layout = $$$"""[{"section":"test","someDate":"{{{date}}}"}]""";
        var translations = _service.Translate(layout, JsonSerializer.Deserialize<Dictionary<string, string>>(Translations), "en");
        Assert.Equal($$$"""[{"section":"test","someDate":"{{{date}}}"}]""", translations.ToJsonString());
    }

    [Fact]
    public void Translate_StringValueWithTranslatedProperty_Translated() {
        var layout = """[{"section":"test","title":"Κωδικός χώρας"}]""";
        var translations = _service.Translate(layout, JsonSerializer.Deserialize<Dictionary<string, string>>(Translations), "en");
        Assert.Equal("""[{"section":"test","title":"Country code"}]""", translations.ToJsonString());
    }

    [Fact]
    public void Translate_StringValueWithNOtTranslatedProperty_Translated() {
        var layout = """[{"section":"test","something":"Κωδικός χώρας"}]""";
        var translations = _service.Translate(layout, JsonSerializer.Deserialize<Dictionary<string, string>>(Translations), "en");
        Assert.NotEqual("""[{"section":"test","something":"Country code"}]""", translations.ToJsonString());
    }

    [Fact]
    public void Translate_ComplexLayout_El_NotTranslated() {
        var translations = _service.Translate(SourceJson, JsonSerializer.Deserialize<Dictionary<string, string>>(Translations), "el");
        Assert.Equal(SourceJson, translations);
    }

    [Fact]
    public void Translate_ComplexLayout_En_Translated() {
        var translations = _service.Translate(SourceJson, JsonSerializer.Deserialize<Dictionary<string, string>>(Translations), "en");

        Assert.DoesNotContain("Θέλω να επικαιροποιήσω το Σταθερό Τηλέφωνο μου", translations.ToJsonString());
        Assert.Contains("I want to validate my home phone", translations.ToJsonString());
    }
}