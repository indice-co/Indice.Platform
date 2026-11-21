using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Services.Risk;
using Indice.Features.Agents.Core.Workflows.Steps.Risk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Tests;

public class RiskEvidenceRedactorTests
{
    private static readonly RiskEvidenceRedactor Masking = new(maskContactDetails: true);
    private static readonly RiskEvidenceRedactor NotMasking = new(maskContactDetails: false);

    [Theory]
    [InlineData("Your code is 482913", "Your code is ••••••")]
    [InlineData("OTP 1234 and 98765432 expire soon", "OTP •••• and •••••••• expire soon")]
    [InlineData("Transfer of 1500.00 EUR needs code 4829.", "Transfer of 1500.00 EUR needs code ••••.")]
    [InlineData("Amount 1,250 EUR", "Amount 1,250 EUR")]                        // too short to be a code
    [InlineData("Call 6912345678 now", "Call 6912345678 now")]                  // ten digits is a phone number, not a code
    [InlineData("Ref 123 and 123456789", "Ref 123 and 123456789")]              // three and nine digits are left alone
    [InlineData("", "")]
    public void MaskOtp_hides_code_like_digit_runs_and_leaves_amounts_and_phone_numbers(string text, string expected) {
        Assert.Equal(expected, RiskEvidenceRedactor.MaskOtp(text));
    }

    [Fact]
    public void MaskOtp_of_null_is_empty() => Assert.Equal(string.Empty, RiskEvidenceRedactor.MaskOtp(null));

    [Theory]
    [InlineData("OtpVerificationCode", true)]
    [InlineData("otpCode", true)]
    [InlineData("SecretCode", true)]
    [InlineData("ClientSecret", true)]
    [InlineData("Password", true)]
    [InlineData("NewPassword", true)]
    [InlineData("PasswordHash", true)]
    [InlineData("SecurityStamp", true)]
    [InlineData("FactorToken", true)]
    [InlineData("accessToken", true)]
    [InlineData("Claims", true)]
    [InlineData("Pin", true)]
    [InlineData("Cvv", true)]
    [InlineData("tokenDeviceId", false)]          // a device id, and useful evidence
    [InlineData("DeviceId", false)]
    [InlineData("PasswordExpired", false)]        // a fact about the account, not a secret
    [InlineData("LastPasswordChangeDate", false)]
    [InlineData("CreditorIban", false)]
    public void IsSecretKey_drops_secrets_and_keeps_facts(string key, bool secret) {
        Assert.Equal(secret, RiskEvidenceRedactor.IsSecretKey(key));
    }

    [Fact]
    public void RedactJson_removes_secrets_at_any_depth_including_inside_arrays() {
        const string json = """
            {"Request":{"CreditorIban":"GR16","OtpVerificationCode":"482913","Nested":{"secretCode":"s3cret","Keep":1}},
             "Items":[{"Password":"x","Name":"a"}],"User":{"Claims":[{"Type":"tin"}],"UserName":"kp"},"tokenDeviceId":"dev-1"}
            """;

        var redacted = Masking.RedactJson(json)!.ToJsonString();

        Assert.DoesNotContain("482913", redacted);
        Assert.DoesNotContain("s3cret", redacted);
        Assert.DoesNotContain("\"Password\"", redacted);
        Assert.DoesNotContain("Claims", redacted);
        Assert.Contains("\"CreditorIban\":\"GR16\"", redacted);
        Assert.Contains("\"Keep\":1", redacted);
        Assert.Contains("\"Name\":\"a\"", redacted);
        Assert.Contains("\"tokenDeviceId\":\"dev-1\"", redacted);
    }

    [Fact]
    public void RedactJson_masks_codes_in_message_bodies_and_contact_details_in_properties() {
        const string json = """{"SmsBody":"Code 482913","PhoneNumber":"+306912345678","Email":"kpapadopoulos@example.com","Other":"482913"}""";

        var redacted = JsonNode.Parse(Masking.RedactJson(json)!.ToJsonString())!.AsObject();

        Assert.Equal("Code ••••••", (string?)redacted["SmsBody"]);
        Assert.Equal("+30•••••••678", (string?)redacted["PhoneNumber"]);
        Assert.Equal("kp••••••@example.com", (string?)redacted["Email"]);
        Assert.Equal("482913", (string?)redacted["Other"]); // only message bodies are scanned for codes
    }

    [Fact]
    public void Contact_masking_can_be_off_but_codes_are_still_masked() {
        const string json = """{"SmsBody":"Code 482913","PhoneNumber":"+306912345678"}""";

        var redacted = JsonNode.Parse(NotMasking.RedactJson(json)!.ToJsonString())!.AsObject();

        Assert.Equal("+306912345678", (string?)redacted["PhoneNumber"]);
        Assert.Equal("Code ••••••", (string?)redacted["SmsBody"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json {")]
    public void RedactJson_of_nothing_or_of_non_json_is_null(string? json) => Assert.Null(Masking.RedactJson(json));

    [Theory]
    [InlineData("+306912345678", "+30•••••••678")]
    [InlineData("+30 691 234 5678", "+30•••••••678")]
    [InlineData("12345", "•••••")]
    public void MaskPhone_keeps_the_prefix_and_the_last_three_characters(string phone, string expected) => Assert.Equal(expected, Masking.MaskPhone(phone));

    [Theory]
    [InlineData("kpapadopoulos@example.com", "kp••••••@example.com")]
    [InlineData("ab@example.com", "ab•@example.com")]
    [InlineData("no-at-sign", "••••••••")]
    public void MaskEmail_keeps_two_characters_and_the_domain(string email, string expected) => Assert.Equal(expected, Masking.MaskEmail(email));

    [Fact]
    public void MaskIdentifier_keeps_the_last_three_characters() => Assert.Equal("••••••789", Masking.MaskIdentifier("123456789"));

    [Fact]
    public void With_masking_off_contact_values_are_returned_unchanged() {
        Assert.Equal("+306912345678", NotMasking.MaskPhone("+306912345678"));
        Assert.Equal("a@b.gr", NotMasking.MaskEmail("a@b.gr"));
        Assert.Equal("123456789", NotMasking.MaskIdentifier("123456789"));
    }
}

public class RiskReportBuilderHelperTests
{
    private static readonly TimeZoneInfo Athens = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

    private static RiskReportBuilder Builder() => new(new RiskEvidenceRedactor(true), Athens, "Europe/Athens", 90, 30);

    [Theory]
    [InlineData("2026-11-12T09:30:15Z", "2026-11-12 09:30:15 UTC (11:30:15 UTC+02:00)")]   // winter
    [InlineData("2026-07-12T09:30:15Z", "2026-07-12 09:30:15 UTC (12:30:15 UTC+03:00)")]   // summer
    public void Times_are_shown_in_utc_and_in_local_time(string utc, string expected) {
        Assert.Equal(expected, Builder().Time(DateTimeOffset.Parse(utc)));
    }

    [Fact]
    public void A_missing_time_is_null() => Assert.Null(Builder().Time(null));

    [Theory]
    [InlineData(null, "—")]
    [InlineData("", "—")]
    [InlineData("   ", "—")]
    [InlineData("plain text", "plain text")]
    [InlineData("a | b", @"a \| b")]
    [InlineData("**bold** _it_ `code`", @"\*\*bold\*\* \_it\_ \`code\`")]
    [InlineData("<script>x</script>", @"\<script\>x\</script\>")]
    [InlineData("[link](http://x)", @"\[link\](http://x)")]
    [InlineData("two\r\nlines\tand   spaces", "two lines and spaces")]
    public void Cell_content_can_never_become_markup_and_stays_on_one_line(string? value, string expected) {
        Assert.Equal(expected, RiskReportBuilder.Cell(value));
    }

    [Fact]
    public void A_very_long_cell_is_cut() {
        var cell = RiskReportBuilder.Cell(new string('x', 5000));

        Assert.True(cell.Length < 450);
        Assert.EndsWith("…", cell);
    }

    [Fact]
    public void Flatten_walks_three_levels_and_names_each_path() {
        var node = JsonNode.Parse("""{"a":{"b":{"c":1}},"d":[10,20],"e":"x"}""")!;

        var fields = RiskReportBuilder.Flatten(node).ToDictionary(f => f.Path, f => f.Value);

        Assert.Equal("1", fields["a.b.c"]);
        Assert.Equal("10", fields["d[0]"]);
        Assert.Equal("20", fields["d[1]"]);
        Assert.Equal("x", fields["e"]);
    }

    [Fact]
    public void Flatten_stops_at_forty_fields_so_a_large_payload_cannot_flood_the_report() {
        var node = new JsonObject();
        for (var i = 0; i < 200; i++) {
            node[$"field{i:000}"] = i;
        }

        Assert.Equal(40, RiskReportBuilder.Flatten(node).Count());
    }

    [Fact]
    public void Flatten_does_not_descend_past_three_levels() {
        var node = JsonNode.Parse("""{"a":{"b":{"c":{"d":"too deep"}}}}""")!;

        var fields = RiskReportBuilder.Flatten(node).ToList();

        Assert.DoesNotContain(fields, f => f.Value == "too deep");
    }
}

public class RiskStepLogicTests
{
    private static readonly DateOnly Today = new(2026, 11, 21);

    [Theory]
    [InlineData("2026-11-12", "2026-11-12")]
    [InlineData("2026-11-21", "2026-11-21")]   // today is allowed
    [InlineData(" 2026-11-12 ", "2026-11-12")]
    [InlineData("2026-11-22", null)]           // tomorrow
    [InlineData("2027-01-01", null)]
    [InlineData("12/11/2026", null)]
    [InlineData("2026-13-01", null)]
    [InlineData("2026-02-30", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void AcceptDate_only_takes_a_real_date_that_is_not_in_the_future(string? candidate, string? expected) {
        Assert.Equal(expected, RiskRequestAnalyzer.AcceptDate(candidate, Today));
    }

    [Fact]
    public void ChooseAnchor_prefers_the_final_event_over_a_step_and_the_earliest_among_equals() {
        var notes = new List<string>();
        var step = ScriptedQueries.Row(new { Id = "step", CreatedAt = "2026-11-12T09:00:00Z", Name = "TransferRequestSecondFactorSent", SubjectId = "s" });
        var late = ScriptedQueries.Row(new { Id = "late", CreatedAt = "2026-11-12T11:00:00Z", Name = "TransferRequest", SubjectId = "s" });
        var early = ScriptedQueries.Row(new { Id = "early", CreatedAt = "2026-11-12T10:00:00Z", Name = "TransferRequest", SubjectId = "s" });

        var chosen = TransactionAnchorResolver.ChooseAnchor([step, late, early], notes, evidenceNumber: 3);

        Assert.Equal("early", chosen.Text("Id"));
        var note = Assert.Single(notes);
        Assert.Contains("3 risk events carry this transaction number", note);
        Assert.DoesNotContain("different subjects", note);
        Assert.Contains("E-3", note);
    }

    [Fact]
    public void ChooseAnchor_with_one_match_adds_no_note() {
        var notes = new List<string>();

        TransactionAnchorResolver.ChooseAnchor([ScriptedQueries.Row(new { Id = "only", CreatedAt = "2026-11-12T10:00:00Z", Name = "TransferRequest", SubjectId = "s" })], notes, 1);

        Assert.Empty(notes);
    }

    [Fact]
    public void ChooseAnchor_says_when_the_events_belong_to_different_subjects() {
        var notes = new List<string>();
        var a = ScriptedQueries.Row(new { Id = "a", CreatedAt = "2026-11-12T10:00:00Z", Name = "TransferRequest", SubjectId = "s1" });
        var b = ScriptedQueries.Row(new { Id = "b", CreatedAt = "2026-11-12T11:00:00Z", Name = "TransferRequest", SubjectId = "s2" });

        TransactionAnchorResolver.ChooseAnchor([a, b], notes, 1);

        Assert.Contains("belonging to 2 different subjects", Assert.Single(notes));
    }

    [Fact]
    public void CitationWarning_lists_only_the_numbers_that_are_not_in_the_log() {
        var riskCase = new RiskCase { Results = [new RiskQueryResult(), new RiskQueryResult(), new RiskQueryResult()] };
        var labels = RiskReportLabels.For("en");

        Assert.Null(RiskReportComposer.CitationWarning("Seen in [E-1] and [E-3].", riskCase, labels));
        Assert.Null(RiskReportComposer.CitationWarning("No citations at all.", riskCase, labels));
        var warning = RiskReportComposer.CitationWarning("Seen in [E-1], [E-4], [E-0] and again [E-4], [E-12].", riskCase, labels)!;
        Assert.Contains("E-0, E-4, E-12", warning);
    }

    [Fact]
    public void RiskCase_numbers_results_from_one_in_the_order_they_ran() {
        var first = new RiskQueryResult { QueryId = "Q-RISK-07" };
        var second = new RiskQueryResult { QueryId = "Q-RISK-01", Rows = [ScriptedQueries.Row(new { Id = "x" })] };
        var riskCase = new RiskCase { Results = [first, second] };

        Assert.Equal(1, riskCase.NumberOf(first));
        Assert.Equal(2, riskCase.NumberOf(second));
        Assert.Equal(0, riskCase.NumberOf(new RiskQueryResult()));
        Assert.Same(first, riskCase.First("q-risk-07"));
        Assert.Equal("x", riskCase.AnchorEvent!.Text("Id"));
    }

    [Fact]
    public void RiskCase_survives_the_json_round_trip_that_checkpoints_put_it_through() {
        var riskCase = new RiskCase {
            Request = new RiskTraceRequest("123", "2026-11-12", "en", "court"),
            Results = [ScriptedQueries.Result("Q-RISK-01", new { Id = "x", Amount = 12.5 }), ScriptedQueries.Failure("Q-ID-04", "boom")],
            Notes = ["a note"]
        };
        riskCase.Results[0].Truncated = true;

        var copy = JsonSerializer.Deserialize<RiskCase>(JsonSerializer.Serialize(riskCase))!;

        Assert.Equal("123", copy.Request.Tun);
        Assert.Equal(2, copy.Results.Count);
        Assert.True(copy.Results[0].Truncated);
        Assert.Equal("x", copy.Results[0].Rows[0].Text("Id"));
        Assert.Equal(12.5m, copy.Results[0].Rows[0].Number("Amount"));
        Assert.Equal("boom", copy.Results[1].Error);
        Assert.False(copy.Results[1].Succeeded);
        Assert.Equal(["a note"], copy.Notes);
    }
}

public class RiskRegistrationTests
{
    private static IConfiguration Configuration(bool withEndpoint = true, params (string Key, string Value)[] extra) {
        var values = new Dictionary<string, string?> {
            ["General:Endpoints:TokenEndpoint"] = "https://identity.example.test/connect/token",
            ["General:Secrets:ClientId"] = "agents", ["General:Secrets:ClientSecret"] = "placeholder", ["General:Secrets:Scope"] = "risk-mcp"
        };
        if (withEndpoint) values["General:Endpoints:RiskMCP"] = "https://risk-mcp.example.test/mcp";
        foreach (var (key, value) in extra) values[key] = value;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void Registration_needs_the_address_of_the_risk_mcp_server() {
        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddRiskInvestigationWorkflow(Configuration(withEndpoint: false)));

        Assert.Contains("General:Endpoints:RiskMCP", exception.Message);
    }

    [Fact]
    public void The_agent_is_registered_with_its_roles_and_its_knowledge_category_from_options() {
        var services = new ServiceCollection();
        services.AddRiskInvestigationWorkflow(Configuration(extra: [("Dex:Risk:RequiredRoles:0", "RiskInvestigator"), ("Dex:Risk:KnowledgeCategory", "antifraud-kb")]));
        using var provider = services.BuildServiceProvider();

        var agent = Assert.Single(provider.GetServices<AgentInfo>(), a => a.Name == AgentsConstants.AgentNames.Risk);

        Assert.Equal(["RiskInvestigator"], agent.RequiredRoles);
        Assert.Equal(["antifraud-kb"], agent.KnowledgeCategories);
        Assert.Contains("evidence report", agent.Description);
    }

    [Fact]
    public void Without_roles_the_agent_fails_instead_of_being_open_to_everyone() {
        var services = new ServiceCollection();
        services.AddRiskInvestigationWorkflow(Configuration());
        using var provider = services.BuildServiceProvider();

        // Options validation fires first; the guard in the agent factory is the backstop if it were ever bypassed. Either way it never resolves.
        var exception = Assert.ThrowsAny<Exception>(() => provider.GetServices<AgentInfo>().ToList());

        Assert.Contains("RequiredRoles", exception.Message);
    }

    [Fact]
    public void Without_roles_the_options_do_not_validate() {
        var services = new ServiceCollection();
        services.AddRiskInvestigationWorkflow(Configuration());
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<RiskInvestigationOptions>>().Value);

        Assert.Contains("RequiredRoles", exception.Message);
    }

    [Fact]
    public void The_workflow_is_registered_under_the_agent_name() {
        var services = new ServiceCollection();
        services.AddRiskInvestigationWorkflow(Configuration(extra: [("Dex:Risk:RequiredRoles:0", "RiskInvestigator")]));

        Assert.Contains(services, d => d.IsKeyedService && Equals(d.ServiceKey, AgentsConstants.AgentNames.Risk));
    }

    [Fact]
    public void Options_can_also_be_configured_in_code() {
        var services = new ServiceCollection();
        services.AddRiskInvestigationWorkflow(Configuration(), o => o.RequiredRoles = ["Legal"]);
        using var provider = services.BuildServiceProvider();

        Assert.Equal(["Legal"], provider.GetRequiredService<IOptions<RiskInvestigationOptions>>().Value.RequiredRoles);
    }

    private static string[] Failures(Action<RiskInvestigationOptions> change) {
        var options = new RiskInvestigationOptions { RequiredRoles = ["Legal"] };
        change(options);
        var result = new RiskInvestigationOptionsValidator().Validate(null, options);
        return result.Failed ? [.. result.Failures] : [];
    }

    [Fact]
    public void Default_options_with_a_role_are_valid() => Assert.Empty(Failures(_ => { }));

    [Fact]
    public void The_validator_rejects_each_unusable_setting() {
        Assert.Contains(Failures(o => o.RequiredRoles = []), f => f.Contains("RequiredRoles"));
        Assert.Contains(Failures(o => o.RequiredRoles = [" "]), f => f.Contains("RequiredRoles"));
        Assert.Contains(Failures(o => o.KnowledgeCategory = ""), f => f.Contains("KnowledgeCategory"));
        Assert.Contains(Failures(o => o.TunPattern = "(unclosed"), f => f.Contains("TunPattern"));
        Assert.Contains(Failures(o => o.MaxInputAttempts = 0), f => f.Contains("MaxInputAttempts"));
        Assert.Contains(Failures(o => o.AnchorSearchWindowDays = []), f => f.Contains("AnchorSearchWindowDays"));
        Assert.Contains(Failures(o => o.AnchorSearchWindowDays = [7, 1]), f => f.Contains("ascending"));
        Assert.Contains(Failures(o => o.AnchorSearchWindowDays = [0, 7]), f => f.Contains("positive"));
        Assert.Contains(Failures(o => o.UsualPatternDays = 0), f => f.Contains("must be positive"));
    }
}
