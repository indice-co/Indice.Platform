using System.Text;
using System.Text.Json;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Steps;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Tests;

/// <summary>
/// Covers the shape of the refusal turn. <see cref="OutOfScopeResponder"/> is the reference producer for every
/// alternative media type, so these assertions are what keep the wire contracts and the chat UI's renderers in step.
/// </summary>
public class OutOfScopeResponderTests
{
    private const string Reason = "I can only answer from the internal documentation.";
    private const string PngDataUriPrefix = "data:image/png;base64,";

    [Fact]
    public void BuildContents_LeadsWithTheReasonAsProse() {
        // Non-UI consumers read DexChatResponse.Text, which is built only from text contents — folding the reason into
        // the callout instead would leave them with an empty answer.
        var text = Assert.IsType<TextContent>(OutOfScopeResponder.BuildContents(Reason, ["faq"])[0]);

        Assert.Equal(Reason, text.Text);
    }

    [Fact]
    public void BuildContents_EmitsEveryRenderableMediaTypeExactlyOnce() {
        var mediaTypes = OutOfScopeResponder.BuildContents(Reason, ["faq"]).OfType<DataContent>()
            .Select(part => part.MediaType).ToList();

        Assert.Equal([
            AgentsConstants.MediaTypes.Callout,
            AgentsConstants.MediaTypes.Image,
            AgentsConstants.MediaTypes.Confirmation,
            AgentsConstants.MediaTypes.MultipleChoice
        ], mediaTypes);
    }

    [Fact]
    public void BuildContents_ImageCarriesTheLogoBytesInlineAsABase64DataUri() {
        // The whole point of the embedded asset: a refusal ships the actual image data, not a link to it. Decoding back
        // to the PNG magic header is what proves a real image travelled — if the embedded resource were ever renamed or
        // dropped from the csproj, the step falls back to a URL and this is the assertion that catches it.
        var image = Payload<ImageReference>(AgentsConstants.MediaTypes.Image);

        Assert.StartsWith(PngDataUriPrefix, image.Url);
        byte[] pngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var bytes = Convert.FromBase64String(image.Url[PngDataUriPrefix.Length..]);
        Assert.Equal(pngMagic, bytes.Take(pngMagic.Length).ToArray());
        Assert.False(string.IsNullOrWhiteSpace(image.Alt));
        Assert.False(string.IsNullOrWhiteSpace(image.Caption));
    }

    [Fact]
    public void BuildContents_InlinedLogoStaysSmallEnoughToShipOnEveryRefusal() {
        // This data URI is streamed and persisted on every refusal turn and re-sent on every load of that conversation,
        // so it is a permanent per-message cost. The SPA's own logo is 819x819 / 1.1 MB — ~1.5 MB base64 — which is why
        // the embedded copy is downscaled. This fails loudly if someone swaps the full-size asset back in.
        var image = Payload<ImageReference>(AgentsConstants.MediaTypes.Image);

        Assert.InRange(image.Url.Length, 1_000, 64_000);
    }

    [Fact]
    public void BuildContents_CalloutCarriesAKnownSeverity() {
        var callout = Payload<Callout>(AgentsConstants.MediaTypes.Callout);

        Assert.Equal(Callout.Severities.Warning, callout.Severity);
        Assert.False(string.IsNullOrWhiteSpace(callout.Text));
    }

    [Fact]
    public void BuildContents_ConfirmationsAffirmativeLabelIsItselfAnInScopeQuestion() {
        // The label is posted verbatim as the next user message, so it has to route somewhere that answers — a label
        // like "Yes" would classify as chit-chat and land straight back on this step.
        var confirmation = Payload<Confirmation>(AgentsConstants.MediaTypes.Confirmation);

        Assert.EndsWith("?", confirmation.ConfirmText);
        Assert.NotEqual(confirmation.ConfirmText, confirmation.CancelText);
    }

    [Fact]
    public void BuildContents_OffersEachSubjectAreaOnceAsAQuestion() {
        // Dex:Taxonomy currently appends to the defaults instead of replacing them, so duplicates reach this step.
        var choice = Payload<MultipleChoice>(AgentsConstants.MediaTypes.MultipleChoice, ["faq", "policy", "FAQ"]);

        Assert.Equal(["What can you tell me about faq?", "What can you tell me about policy?"], choice.Options);
    }

    [Fact]
    public void BuildContents_OmitsTheChoiceListWhenNoSubjectAreasAreConfigured() {
        var mediaTypes = OutOfScopeResponder.BuildContents(Reason, []).OfType<DataContent>()
            .Select(part => part.MediaType);

        Assert.DoesNotContain(AgentsConstants.MediaTypes.MultipleChoice, mediaTypes);
    }

    [Fact]
    public void BuildContents_PartsSurviveTheProjectionTheClientActuallyReceives() {
        // Every payload has to come back out as raw JSON, not a base64 data: URI — that is what the renderers parse.
        var parts = new ChatMessage(ChatRole.Assistant, OutOfScopeResponder.BuildContents(Reason, ["faq"]))
            .ToDexChatMessage().Content.Parts;

        Assert.Equal(5, parts.Count); // the prose, then one part per media type
        Assert.All(parts.Skip(1), part => Assert.StartsWith("{", part.Value));
    }

    private static TPayload Payload<TPayload>(string mediaType, IEnumerable<string>? categories = null) {
        var part = OutOfScopeResponder.BuildContents(Reason, categories ?? ["faq"]).OfType<DataContent>()
            .Single(content => content.MediaType == mediaType);
        return JsonSerializer.Deserialize<TPayload>(Encoding.UTF8.GetString(part.Data.Span))!;
    }
}
