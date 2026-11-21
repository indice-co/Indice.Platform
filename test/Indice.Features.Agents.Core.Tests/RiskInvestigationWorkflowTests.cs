using System.Security.Cryptography;
using System.Text;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services.Risk;
using Indice.Features.Agents.Core.Workflows.Steps.Risk;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Tests;

/// <summary>
/// The risk investigation end to end: the real workflow graph and checkpoints, with the model, the MCP server and the knowledge base
/// scripted. These tests pin what a legal report depends on: which queries run and in what order, that secrets and one-time passwords
/// never reach the report, that a number the model invented is never looked up, and that only the permitted role gets in.
/// </summary>
public class RiskInvestigationWorkflowTests
{
    private const string Tun = RiskHarness.KnownTun;

    private static readonly string[] ExpectedQueryOrder = [
        "Q-RISK-07", "Q-RISK-01",
        "Q-RISK-06", "Q-RISK-02", "Q-RISK-02", "Q-RISK-02", "Q-RISK-03",
        "Q-SIGN-02", "Q-ID-04",
        "Q-RISK-08",
        "Q-RISK-10", "Q-ACT-01",
        "Q-RISK-19", "Q-SIGN-06",
        "Q-ID-01", "Q-ID-03",
        "Q-RISK-17", "Q-SIGN-07", "Q-ACT-05"
    ];

    private static async Task<(RiskHarness Harness, ChatResponse Response)> RunTransferAsync(Action<RiskHarness>? tweak = null, Action<Dictionary<string, object>>? tweakAnchor = null) {
        var harness = RiskHarness.Create();
        harness.Queries.ScriptTransfer(tweakAnchor);
        tweak?.Invoke(harness);
        var response = await harness.SendAsync($"Φάκελος για το δικαστήριο για τη συναλλαγή {Tun} της 12/11");
        return (harness, response);
    }

    // ---- the report ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_complete_request_produces_the_report_and_two_downloads() {
        var (harness, response) = await RunTransferAsync();
        await using var _ = harness;

        Assert.Null(RiskHarness.AskedFor(response));
        var files = RiskHarness.FilesIn(response);
        Assert.Equal(2, files.Count);
        var html = Assert.Single(files, f => f.ContentType == "text/html");
        var markdown = Assert.Single(files, f => f.ContentType == "text/markdown");
        Assert.EndsWith(".html", html.FileName);
        Assert.EndsWith(".md", markdown.FileName);
        Assert.Contains(Tun, markdown.FileName);

        var text = response.Text;
        Assert.Contains($"TUN {Tun}", text);
        Assert.Contains("The report is ready", text);
        // Header, then the findings summary, then the evidence, in the order a reader meets them.
        Assert.True(text.IndexOf("Findings summary") < text.IndexOf("The customer logged in"));
        Assert.True(text.IndexOf("The customer logged in") < text.IndexOf("Customer"));
    }

    [Fact]
    public async Task The_query_chain_follows_playbook_P_02_in_order() {
        var (harness, _) = await RunTransferAsync();
        await using var _ = harness;

        Assert.Equal(ExpectedQueryOrder, harness.Queries.QueryIds);
    }

    [Fact]
    public async Task The_transaction_is_looked_up_in_a_window_around_the_given_date_in_local_time() {
        var (harness, _) = await RunTransferAsync();
        await using var _ = harness;

        var lookup = harness.Queries.Calls.First(call => call.QueryId == "Q-RISK-07").Parameters;
        Assert.Equal(Tun, lookup["Tun"]);
        // 12 November in Athens is UTC+2, so the day runs from 22:00Z on the 11th to 22:00Z on the 12th.
        Assert.Equal(new DateTimeOffset(2026, 11, 11, 22, 0, 0, TimeSpan.Zero), lookup["From"]);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 22, 0, 0, TimeSpan.Zero), lookup["To"]);
    }

    [Fact]
    public async Task Evidence_queries_are_anchored_on_the_event_found_not_on_what_the_user_typed() {
        var (harness, _) = await RunTransferAsync();
        await using var _ = harness;

        var calls = harness.Queries.Calls;
        Assert.Equal(RiskHarness.AnchorEventId, calls.First(c => c.QueryId == "Q-RISK-01").Parameters["EventId"]);
        Assert.Equal("pwd.8f1c", calls.First(c => c.QueryId == "Q-RISK-06").Parameters["SessionId"]);
        Assert.Equal("TransferRequest", calls.First(c => c.QueryId == "Q-RISK-06").Parameters["BaseName"]);
        Assert.Equal("subject-1", calls.First(c => c.QueryId == "Q-ID-01").Parameters["SubjectId"]);
        var anchorTime = new DateTimeOffset(2026, 11, 12, 9, 30, 15, TimeSpan.Zero);
        var login = calls.First(c => c.QueryId == "Q-SIGN-02").Parameters;
        Assert.Equal(anchorTime.AddDays(-1), login["From"]);
        Assert.Equal(anchorTime.AddHours(1), login["To"]);
        var usual = calls.First(c => c.QueryId == "Q-RISK-19").Parameters;
        Assert.Equal(anchorTime.AddDays(-90), usual["From"]);
        Assert.Equal(anchorTime, usual["To"]);
    }

    [Fact]
    public async Task Secrets_and_one_time_passwords_never_reach_the_report() {
        var (harness, response) = await RunTransferAsync();
        await using var _ = harness;
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));
        var html = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/html"));

        foreach (var document in new[] { response.Text, markdown, html }) {
            Assert.DoesNotContain("482913", document);           // the OTP, in the event data and in the SMS body
            Assert.DoesNotContain("OtpVerificationCode", document);
            Assert.DoesNotContain("FactorToken", document);
            Assert.DoesNotContain("abc.def", document);
            Assert.DoesNotContain("123456789", document);        // the full tax id
            Assert.DoesNotContain("profile_note", document);     // a claim that is not on the list
        }
        // What is evidence stays: the beneficiary account and the amount of the message.
        Assert.Contains("GR1601101250000000012300695", markdown);
        Assert.Contains("1500.00 EUR", markdown);
    }

    [Fact]
    public async Task Contact_details_are_partially_masked_by_default() {
        var (harness, response) = await RunTransferAsync();
        await using var _ = harness;
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.DoesNotContain("+306912345678", markdown);
        Assert.Contains("+30•••••••678", markdown); // 13 characters: 3 kept, 7 masked, 3 kept
        Assert.DoesNotContain("kpapadopoulos@example.com", markdown);
        Assert.Contains("kp••••••@example.com", markdown);
        Assert.Contains("••••••789", markdown);
    }

    [Fact]
    public async Task Masking_can_be_turned_off_for_a_host_that_needs_the_full_values() {
        var harness = RiskHarness.Create(configure: o => o.MaskContactDetails = false);
        await using var _ = harness;
        harness.Queries.ScriptTransfer();
        var response = await harness.SendAsync($"Report for transaction {Tun} on 12/11");
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.Contains("+306912345678", markdown);
        Assert.DoesNotContain("482913", markdown); // codes are masked whatever this is set to
    }

    [Fact]
    public async Task The_report_cites_every_query_in_an_annex_with_the_hash_of_its_sql() {
        var (harness, response) = await RunTransferAsync();
        await using var _ = harness;
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.Contains("Annex A: queries executed", markdown);
        for (var number = 1; number <= ExpectedQueryOrder.Length; number++) {
            Assert.Contains($"| E-{number} |", markdown);
        }
        foreach (var id in ExpectedQueryOrder.Distinct()) {
            Assert.Contains(id, markdown);
        }
        Assert.Contains(new string('a', 64), markdown);
        Assert.DoesNotContain("Q-RISK-09", markdown); // a transfer is not a card payment
    }

    [Fact]
    public async Task Limitations_are_quoted_from_the_knowledge_base_and_a_missing_one_is_said_to_be_missing() {
        var (harness, response) = await RunTransferAsync(h => h.Knowledge.Limitations.RemoveAll(s => s.HeadingPath.Contains("L-04")));
        await using var _ = harness;
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.Contains("Verbatim text of L-01", markdown);
        Assert.Contains("Verbatim text of L-13", markdown);
        Assert.Contains("**L-04**: The text was not found in the knowledge base.", markdown);
        Assert.Equal(RiskReportBuilder.LimitationIds, harness.Knowledge.LastPrefixes);
        Assert.True(harness.Knowledge.LastHeadingIncludeRestricted); // the limitations live in the restricted category
    }

    [Fact]
    public async Task A_knowledge_base_that_is_down_does_not_stop_the_report() {
        var (harness, response) = await RunTransferAsync(h => h.Knowledge.Fail = true);
        await using var _ = harness;

        Assert.Equal(2, RiskHarness.FilesIn(response).Count);
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));
        Assert.Contains("**L-01**: The text was not found in the knowledge base.", markdown);
    }

    [Fact]
    public async Task The_html_file_carries_the_hash_of_the_markdown_file() {
        var (harness, response) = await RunTransferAsync();
        await using var _ = harness;
        var files = RiskHarness.FilesIn(response);
        var markdownFile = files.Single(f => f.ContentType == "text/markdown");
        var html = await harness.ReadFileAsync(files.Single(f => f.ContentType == "text/html"));
        var markdown = await harness.ReadFileAsync(markdownFile);

        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(markdown))).ToLowerInvariant();
        Assert.Equal(sha, markdownFile.Sha256);
        Assert.Contains(sha, html);
        Assert.StartsWith("<!doctype html>", html);
        Assert.Contains("Content-Security-Policy", html);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Text_that_came_from_a_log_can_never_become_markup() {
        var (harness, response) = await RunTransferAsync(h => {
            var original = h.Queries.Handlers["Q-RISK-08"];
            h.Queries.Handlers["Q-RISK-08"] = p => ScriptedQueries.Result("Q-RISK-08", new {
                CreatedAt = "2026-11-12T09:28:41.0000000Z", Name = "SmsSentEvent", DeliveryChannel = "Sms", TrackingId = "t",
                SmsBody = "<script>alert(document.cookie)</script> | **bold** [link](http://evil.example)"
            });
        });
        await using var _ = harness;
        var html = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/html"));

        Assert.DoesNotContain("<script>alert", html);
        Assert.Contains("&lt;script&gt;alert", html);
        Assert.DoesNotContain("<a href=\"http://evil.example\"", html);
        Assert.DoesNotContain("<strong>bold</strong>", html);
    }

    [Fact]
    public async Task The_report_is_written_in_greek_when_the_user_writes_greek() {
        var (harness, response) = await RunTransferAsync(h => h.Model.Analyze = _ => new RiskRequestAnalysis("P-02", Tun, "2026-11-12", "el", "δικαστήριο"));
        await using var _ = harness;
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.Contains("Φάκελος τεκμηρίωσης συναλλαγής", markdown);
        Assert.Contains("Σύνοψη ευρημάτων", markdown);
        Assert.Contains("Ο φάκελος είναι έτοιμος", response.Text);
        Assert.Contains(harness.Model.NarrativeInstructions!, "[RiskReportNarrative]");
    }

    [Fact]
    public async Task The_model_only_sees_the_redacted_evidence() {
        var (harness, _) = await RunTransferAsync();
        await using var _ = harness;

        var input = harness.Model.NarrativeInput!;
        Assert.Contains("[E-", input);
        Assert.Contains("data from the bank's logs", input);
        Assert.DoesNotContain("482913", input);
        Assert.DoesNotContain("OtpVerificationCode", input);
        Assert.DoesNotContain("+306912345678", input);
        Assert.DoesNotContain("Findings summary", input); // it writes that section; it does not read it back
    }

    // ---- the findings summary -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_summary_citing_a_query_that_does_not_exist_is_flagged() {
        var (harness, response) = await RunTransferAsync(h => h.Model.Narrative = "The transfer was made from a registered device [E-2] and [E-99].");
        await using var _ = harness;
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.Contains("cites sources that are not in the annex: E-99", markdown);
    }

    [Fact]
    public async Task A_summary_that_cites_only_real_queries_is_not_flagged() {
        var (harness, response) = await RunTransferAsync();
        await using var _ = harness;

        Assert.DoesNotContain("not in the annex", response.Text);
    }

    [Fact]
    public async Task A_failing_summary_does_not_stop_the_report() {
        var (harness, response) = await RunTransferAsync(h => h.Model.FailNarrative = true);
        await using var _ = harness;

        Assert.Equal(2, RiskHarness.FilesIn(response).Count);
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));
        Assert.Contains("The summary was not available at this time", markdown);
        Assert.Contains("Annex A", markdown); // every evidence section is still there
    }

    // ---- failures in the data -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_failed_query_marks_its_section_incomplete_and_the_report_is_still_produced() {
        var (harness, response) = await RunTransferAsync(h => h.Queries.Handlers["Q-ID-04"] = _ => ScriptedQueries.Failure("Q-ID-04", "SQL error 4060"));
        await using var _ = harness;
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.Contains("Incomplete", markdown);
        Assert.Contains("Q-ID-04", markdown);
        Assert.Contains("SQL error 4060", markdown);
        Assert.Contains("1 query(ies) failed; the corresponding sections are incomplete.", markdown);
        Assert.Equal(ExpectedQueryOrder, harness.Queries.QueryIds); // the chain carries on after a failure
    }

    [Fact]
    public async Task A_truncated_result_is_flagged_as_partial() {
        var (harness, response) = await RunTransferAsync(h => h.Queries.Handlers["Q-RISK-19"] = _ => {
            var result = ScriptedQueries.Result("Q-RISK-19", new { IpAddress = "203.0.113.9", CountryIsoCode = "GR", Location = "Athens", Events = 1, FirstSeen = "2026-10-01T00:00:00Z", LastSeen = "2026-10-02T00:00:00Z" });
            result.Truncated = true;
            return result;
        });
        await using var _ = harness;
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.Contains("more rows exist than are shown", markdown);
        Assert.Contains("1 result(s) were truncated", markdown);
    }

    [Fact]
    public async Task An_anchor_without_a_session_is_linked_by_time_and_the_report_says_so() {
        var (harness, response) = await RunTransferAsync(tweakAnchor: anchor => anchor["SessionId"] = null!);
        await using var _ = harness;
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.Contains("linked by time", markdown);
        Assert.Contains("Q-RISK-05", harness.Queries.QueryIds);
        Assert.DoesNotContain("Q-RISK-06", harness.Queries.QueryIds);
        Assert.DoesNotContain("Q-SIGN-02", harness.Queries.QueryIds);
    }

    [Fact]
    public async Task A_card_payment_also_reads_the_card_verification_events() {
        var (harness, _) = await RunTransferAsync(h => h.Queries.Handlers["Q-RISK-09"] = _ => ScriptedQueries.Result("Q-RISK-09"), anchor => anchor["Name"] = "CardPaymentRequest");
        await using var _ = harness;

        Assert.Contains("Q-RISK-09", harness.Queries.QueryIds);
    }

    [Fact]
    public async Task A_broken_connection_to_the_server_stops_the_run_instead_of_producing_a_partial_report() {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Queries.ScriptTransfer();
        harness.Queries.Handlers["Q-RISK-01"] = _ => throw new RiskQueryException("The risk MCP server could not be reached while running Q-RISK-01.");

        var response = await harness.SendAsync($"Report for {Tun} on 12/11");

        Assert.Empty(RiskHarness.FilesIn(response));
        Assert.DoesNotContain("The report is ready", response.Text);
    }

    // ---- asking the user ----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_missing_date_is_asked_for_and_the_answer_completes_the_report() {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Queries.ScriptTransfer();
        harness.Model.Analyze = text => new RiskRequestAnalysis("P-02", text.Contains(Tun) ? Tun : null, text.Contains("12/11") ? "2026-11-12" : null, "en", null);

        var first = await harness.SendAsync($"Evidence pack for transaction {Tun}");

        Assert.NotNull(RiskHarness.AskedFor(first));
        Assert.Contains("date of the transaction", first.Text);
        Assert.Empty(harness.Queries.Calls); // nothing is looked up until the request is complete

        var second = await harness.ReplyAsync(first, "it was on 12/11");

        Assert.Null(RiskHarness.AskedFor(second));
        Assert.Equal(2, RiskHarness.FilesIn(second).Count);
        Assert.Equal(ExpectedQueryOrder, harness.Queries.QueryIds);
        // The number from the first turn was kept; the user did not have to repeat it.
        Assert.Equal(Tun, harness.Queries.Calls.First(c => c.QueryId == "Q-RISK-07").Parameters["Tun"]);
        Assert.Equal(2, harness.Model.AnalyzerInputs.Count);
    }

    [Fact]
    public async Task A_missing_number_is_asked_for() {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Model.Analyze = _ => new RiskRequestAnalysis("P-02", null, "2026-11-12", "en", null);

        var response = await harness.SendAsync("I need a report for a transaction on 12/11");

        Assert.NotNull(RiskHarness.AskedFor(response));
        Assert.Contains("transaction number (TUN)", response.Text);
        Assert.Empty(harness.Queries.Calls);
    }

    [Fact]
    public async Task A_number_the_model_invented_is_never_looked_up() {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        // The user wrote no number at all; the model "extracts" one anyway.
        harness.Model.Analyze = _ => new RiskRequestAnalysis("P-02", "999888777", "2026-11-12", "en", null);

        var response = await harness.SendAsync("Please prepare the court report for the disputed transfer of 12/11");

        Assert.Empty(harness.Queries.Calls);
        Assert.NotNull(RiskHarness.AskedFor(response));
        Assert.Contains("999888777", response.Text); // it tells the user which value it refused
        Assert.Contains("not shaped like", response.Text);
    }

    [Theory]
    [InlineData("12 34")]                              // whitespace
    [InlineData("1'; DROP TABLE RiskEvent;--")]       // injection attempt
    [InlineData("ab")]                                 // too short
    public async Task A_number_that_is_not_shaped_like_a_tun_is_never_looked_up(string tun) {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Model.Analyze = text => new RiskRequestAnalysis("P-02", tun, "2026-11-12", "en", null);

        var response = await harness.SendAsync($"Report for transaction {tun} on 12/11");

        Assert.Empty(harness.Queries.Calls);
        Assert.NotNull(RiskHarness.AskedFor(response));
    }

    [Theory]
    [InlineData("2026-11-22")]  // tomorrow
    [InlineData("12/11/2026")]  // not the format the model was asked for
    [InlineData("not a date")]
    public async Task A_date_that_is_in_the_future_or_unreadable_is_asked_for_again(string date) {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Model.Analyze = _ => new RiskRequestAnalysis("P-02", Tun, date, "en", null);

        var response = await harness.SendAsync($"Report for transaction {Tun}");

        Assert.Empty(harness.Queries.Calls);
        Assert.NotNull(RiskHarness.AskedFor(response));
        Assert.Contains("date", response.Text);
    }

    [Fact]
    public async Task The_agent_gives_up_after_the_attempt_limit() {
        var harness = RiskHarness.Create(configure: o => o.MaxInputAttempts = 1);
        await using var _ = harness;
        harness.Model.Analyze = _ => new RiskRequestAnalysis("P-02", null, null, "en", null);

        var first = await harness.SendAsync("I need a report");
        Assert.NotNull(RiskHarness.AskedFor(first));

        var second = await harness.ReplyAsync(first, "I do not know");

        Assert.Null(RiskHarness.AskedFor(second));
        Assert.Contains("I could not gather the details I need", second.Text);
        Assert.Empty(harness.Queries.Calls);
    }

    // ---- not found ----------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_transaction_that_is_not_found_widens_the_window_once_then_asks_again() {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Queries.ScriptTransfer();
        harness.Model.Analyze = _ => new RiskRequestAnalysis("P-02", "9000000000", "2026-11-12", "en", null);

        var response = await harness.SendAsync("Report for transaction 9000000000 on 12/11");

        Assert.Equal(["Q-RISK-07", "Q-RISK-07", "Q-RISK-17"], harness.Queries.QueryIds);
        var narrow = harness.Queries.Calls[0].Parameters;
        var wide = harness.Queries.Calls[1].Parameters;
        Assert.Equal(TimeSpan.FromDays(1), (DateTimeOffset)narrow["To"]! - (DateTimeOffset)narrow["From"]!);
        Assert.Equal(TimeSpan.FromDays(7), (DateTimeOffset)wide["To"]! - (DateTimeOffset)wide["From"]!);
        Assert.NotNull(RiskHarness.AskedFor(response));
        Assert.Contains("could not find a transaction event with TUN 9000000000", response.Text);
        Assert.Contains("(L-05)", response.Text); // events exist in that period, so the event is missing
        Assert.Empty(RiskHarness.FilesIn(response));
    }

    [Fact]
    public async Task When_the_risk_api_recorded_nothing_the_user_is_told_it_may_not_have_been_collecting() {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Queries.ScriptTransfer();
        harness.Queries.Handlers["Q-RISK-17"] = _ => ScriptedQueries.Result("Q-RISK-17");
        harness.Model.Analyze = _ => new RiskRequestAnalysis("P-02", "9000000000", "2026-11-12", "en", null);

        var response = await harness.SendAsync("Report for transaction 9000000000 on 12/11");

        Assert.Contains("may not have been collecting then (L-01)", response.Text);
    }

    [Fact]
    public async Task After_a_miss_a_new_date_finds_the_transaction_and_completes_the_report() {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Queries.ScriptTransfer();
        harness.Model.Analyze = text => new RiskRequestAnalysis("P-02", text.Contains(Tun) ? Tun : null, text.Contains("12/11") ? "2026-11-12" : text.Contains("5/11") ? "2026-11-05" : null, "en", null);
        var transactionTime = new DateTimeOffset(2026, 11, 5, 9, 30, 15, TimeSpan.Zero);
        harness.Queries.Handlers["Q-RISK-07"] = p => !((DateTimeOffset)p["From"]! <= transactionTime && transactionTime < (DateTimeOffset)p["To"]!)
            ? ScriptedQueries.Result("Q-RISK-07")
            : ScriptedQueries.Result("Q-RISK-07", new { Id = RiskHarness.AnchorEventId.ToString(), CreatedAt = "2026-11-05T09:30:15.0000000Z", SubjectId = "subject-1", SessionId = "pwd.8f1c", Name = "TransferRequest", Type = "Transfer", Amount = 1500.0, IpAddress = "203.0.113.9" });

        var first = await harness.SendAsync($"Report for transaction {Tun} on 12/11");
        Assert.NotNull(RiskHarness.AskedFor(first));
        var second = await harness.ReplyAsync(first, "sorry, it was on 5/11");

        Assert.Null(RiskHarness.AskedFor(second));
        Assert.Equal(2, RiskHarness.FilesIn(second).Count);
    }

    [Fact]
    public async Task Several_events_with_the_same_number_are_reported_as_ambiguous_and_the_earliest_is_used() {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Queries.ScriptTransfer();
        harness.Queries.Handlers["Q-RISK-07"] = _ => ScriptedQueries.Result("Q-RISK-07",
            new { Id = "99999999-9999-9999-9999-999999999999", CreatedAt = "2026-11-12T11:00:00Z", SubjectId = "subject-2", SessionId = "s-2", Name = "TransferRequest", Type = "Transfer", Amount = 5.0, IpAddress = "198.51.100.1" },
            new { Id = RiskHarness.AnchorEventId.ToString(), CreatedAt = "2026-11-12T09:30:15Z", SubjectId = "subject-1", SessionId = "pwd.8f1c", Name = "TransferRequest", Type = "Transfer", Amount = 1500.0, IpAddress = "203.0.113.9" });

        var response = await harness.SendAsync($"Report for transaction {Tun} on 12/11");
        var markdown = await harness.ReadFileAsync(RiskHarness.FilesIn(response).Single(f => f.ContentType == "text/markdown"));

        Assert.Equal(RiskHarness.AnchorEventId, harness.Queries.Calls.First(c => c.QueryId == "Q-RISK-01").Parameters["EventId"]);
        Assert.Contains("2 risk events carry this transaction number, belonging to 2 different subjects", markdown);
    }

    // ---- who may use it, and what it declines -------------------------------------------------------------------------------

    [Fact]
    public async Task A_caller_without_the_role_gets_nothing_and_no_query_runs() {
        var harness = RiskHarness.Create(RiskHarness.Investigator(role: "BackOfficeClerk"), o => o.DefaultLanguage = "en");
        await using var _ = harness;
        harness.Queries.ScriptTransfer();

        var response = await harness.SendAsync($"Report for transaction {Tun} on 12/11");

        Assert.Contains("do not have access", response.Text);
        Assert.Empty(harness.Queries.Calls);
        Assert.Empty(harness.Model.AnalyzerInputs); // not even the model is asked
        Assert.Empty(RiskHarness.FilesIn(response));
    }

    [Fact]
    public async Task An_anonymous_caller_gets_nothing() {
        var harness = RiskHarness.Create(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity()), o => o.DefaultLanguage = "en");
        await using var _ = harness;

        var response = await harness.SendAsync($"Report for transaction {Tun} on 12/11");

        Assert.Contains("do not have access", response.Text);
        Assert.Empty(harness.Queries.Calls);
    }

    [Fact]
    public async Task Without_a_language_to_go_on_a_refusal_is_in_the_configured_default_language() {
        // The refusal happens before anything is read from the user, so the host's default language is all there is to use.
        var harness = RiskHarness.Create(RiskHarness.Investigator(role: "BackOfficeClerk"));
        await using var _ = harness;

        var response = await harness.SendAsync($"Report for transaction {Tun} on 12/11");

        Assert.Contains("Δεν έχετε πρόσβαση σε αυτή τη λειτουργία.", response.Text);
        Assert.Empty(harness.Queries.Calls);
    }

    [Theory]
    [InlineData("P-01")]
    [InlineData("P-04")]
    [InlineData("P-08")]
    public async Task Another_report_type_is_declined_with_a_clear_reply(string playbook) {
        var harness = RiskHarness.Create();
        await using var _ = harness;
        harness.Model.Analyze = _ => new RiskRequestAnalysis(playbook, Tun, "2026-11-12", "en", null);

        var response = await harness.SendAsync($"Customer timeline for {Tun}");

        Assert.Contains("playbook P-02", response.Text);
        Assert.Contains($"playbook {playbook}", response.Text);
        Assert.Empty(harness.Queries.Calls);
        Assert.Empty(RiskHarness.FilesIn(response));
    }

    // ---- plumbing -----------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task The_conversation_id_reaches_the_server_as_the_correlation_id_and_each_step_opens_one_session() {
        var (harness, _) = await RunTransferAsync();
        await using var _ = harness;

        Assert.Equal(harness.ConversationId.ToString(), harness.Queries.CorrelationId);
        Assert.Equal(2, harness.Queries.Sessions); // the resolver and the collector
    }

    [Fact]
    public async Task The_analyzer_reads_the_antifraud_knowledge_with_the_restricted_opt_in() {
        var (harness, _) = await RunTransferAsync();
        await using var _ = harness;

        Assert.Equal("antifraud-kb", harness.Knowledge.LastCategory);
        Assert.True(harness.Knowledge.LastIncludeRestricted);
    }

    [Fact]
    public async Task The_files_are_only_readable_by_the_user_who_asked_for_the_report() {
        var (harness, response) = await RunTransferAsync();
        await using var _ = harness;
        var file = RiskHarness.FilesIn(response)[0];

        Assert.NotNull(await harness.Files.GetAsync(harness.ConversationId, file.Id, RiskHarness.UserId, TestContext.Current.CancellationToken));
        Assert.Null(await harness.Files.GetAsync(harness.ConversationId, file.Id, "another-user", TestContext.Current.CancellationToken));
    }
}
