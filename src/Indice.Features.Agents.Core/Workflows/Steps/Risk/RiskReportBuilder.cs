using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services.Risk;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>What the report needs besides the evidence.</summary>
/// <param name="ReportId">The report id shown in the header.</param>
/// <param name="GeneratedAt">When the report was generated.</param>
/// <param name="RequestedBy">Who asked for it.</param>
/// <param name="Limitations">The limitation sections read from the knowledge base, quoted verbatim.</param>
internal sealed record RiskReportContext(string ReportId, DateTimeOffset GeneratedAt, string RequestedBy, IReadOnlyList<KnowledgeSnippet> Limitations);

/// <summary>The report in the parts it is assembled from: the findings summary goes between <see cref="SummaryHeading"/> and <see cref="Evidence"/>.</summary>
/// <param name="Title">The document title.</param>
/// <param name="Header">The title and the table of facts about the report.</param>
/// <param name="SummaryHeading">The heading of the findings summary, with its disclaimer.</param>
/// <param name="Evidence">The evidence sections, built only from query results.</param>
/// <param name="Closing">The limitations and the annex of executed queries.</param>
internal sealed record RiskReportParts(string Title, string Header, string SummaryHeading, string Evidence, string Closing);

/// <summary>
/// Builds the evidence report from the results of the investigation's queries. Every table and every comparison sentence is produced
/// here, deterministically, from the rows; a model only writes the findings summary. The same case always produces the same report,
/// and each section names the queries (<c>E-n</c>) it was built from.
/// </summary>
internal sealed partial class RiskReportBuilder
{
    /// <summary>The limitation sections of the knowledge base (file 09) that apply to a disputed-transaction report: playbook P-02 plus L-06, since the report shows scores.</summary>
    public static readonly IReadOnlyList<string> LimitationIds = ["L-01", "L-02", "L-03", "L-04", "L-05", "L-06", "L-09", "L-11", "L-13"];

    private const int MaxCellLength = 400;

    private static readonly string[] CustomerClaims = ["customer_code", "bank_customer_id", "given_name", "family_name", "tin", "phone1", "phone2"];

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();

    private readonly RiskEvidenceRedactor _redactor;
    private readonly TimeZoneInfo _zone;
    private readonly string _zoneId;
    private readonly int _usualPatternDays;
    private readonly int _priorChangesDays;

    /// <summary>Creates a builder.</summary>
    public RiskReportBuilder(RiskEvidenceRedactor redactor, TimeZoneInfo zone, string zoneId, int usualPatternDays, int priorChangesDays) {
        _redactor = redactor;
        _zone = zone;
        _zoneId = zoneId;
        _usualPatternDays = usualPatternDays;
        _priorChangesDays = priorChangesDays;
    }

    /// <summary>Builds the parts of the report.</summary>
    public RiskReportParts Build(RiskCase riskCase, RiskReportContext context) {
        var l = RiskReportLabels.For(riskCase.Request.Language);
        var anchor = riskCase.AnchorEvent;
        var title = $"{l["title"]}: TUN {riskCase.Request.Tun}";

        var header = new StringBuilder();
        header.AppendLine($"# {Escape(title)}").AppendLine();
        header.AppendLine(Table([l["c.field"], l["c.value"]], [
            [l["reportId"], Cell(context.ReportId)],
            [l["generated"], Cell(Time(context.GeneratedAt))],
            [l["requestedBy"], Cell(context.RequestedBy)],
            [l["purpose"], Cell(riskCase.Request.Purpose)],
            [l["tun"], Cell(riskCase.Request.Tun)],
            [l["anchorEvent"], Cell(anchor is null ? null : $"{anchor.Text("Id")} ({anchor.Text("Name")})")]
        ]));
        header.AppendLine().AppendLine(l.Format("timeNote", _zoneId)).AppendLine();

        var summaryHeading = $"## 1. {l["summary"]}\n\n> {l["summaryNote"]}\n";

        var evidence = new StringBuilder();
        var number = 2;
        AppendSection(evidence, number++, l["customer"], Customer(riskCase, l));
        AppendSection(evidence, number++, l["transaction"], Transaction(riskCase, l));
        AppendSection(evidence, number++, l["authentication"], Authentication(riskCase, l));
        AppendSection(evidence, number++, l["otp"], Notifications(riskCase, l));
        AppendSection(evidence, number++, l["network"], Network(riskCase, l));
        AppendSection(evidence, number++, l["changes"], Changes(riskCase, l));
        AppendSection(evidence, number++, l["coverage"], Coverage(riskCase, l));
        if (riskCase.Notes.Count > 0) {
            AppendSection(evidence, number++, l["dataNotes"], string.Join("\n", riskCase.Notes.Select(note => $"- {Escape(note)}")));
        }

        var closing = new StringBuilder();
        AppendSection(closing, number++, l["limitations"], Limitations(riskCase, context, l));
        AppendSection(closing, number, l["annex"], Annex(riskCase, l));
        return new RiskReportParts(title, header.ToString(), summaryHeading, evidence.ToString(), closing.ToString());
    }

    // ---- sections ---------------------------------------------------------------------------------------------------------

    private string Customer(RiskCase c, RiskReportLabels l) {
        var sb = new StringBuilder();
        var user = c.First("Q-ID-01");
        if (user is { Succeeded: true, Rows: [var row] }) {
            sb.AppendLine(Table([l["c.field"], l["c.value"]], [
                [l["f.subject"], Cell(row.Text("Id"))],
                [l["f.userName"], Cell(row.Text("UserName"))],
                [l["f.email"], Cell(_redactor.MaskEmail(row.Text("Email")))],
                [l["f.emailConfirmed"], Cell(YesNo(row.Flag("EmailConfirmed"), l))],
                [l["f.phone"], Cell(_redactor.MaskPhone(row.Text("PhoneNumber")))],
                [l["f.phoneConfirmed"], Cell(YesNo(row.Flag("PhoneNumberConfirmed"), l))],
                [l["f.twoFactor"], Cell(YesNo(row.Flag("TwoFactorEnabled"), l))],
                [l["f.blocked"], Cell(YesNo(row.Flag("Blocked"), l))],
                [l["f.created"], Cell(Time(row.Time("CreateDate")))],
                [l["f.lastSignIn"], Cell(Time(row.Time("LastSignInDate")))],
                [l["f.lastPassword"], Cell(Time(row.Time("LastPasswordChangeDate")))]
            ]));
            sb.AppendLine().AppendLine($"{l["source"]}: {Tag(c, user)}");
        } else if (user is { Succeeded: true }) {
            sb.AppendLine($"{l["noUser"]} {Tag(c, user)}");
        } else {
            sb.AppendLine(Unavailable(c, user, l));
        }

        sb.AppendLine().AppendLine($"### {l["h.claims"]}").AppendLine();
        var claims = c.First("Q-ID-03");
        if (claims is { Succeeded: true }) {
            var shown = claims.Rows.Where(r => CustomerClaims.Contains(r.Text("ClaimType") ?? string.Empty, StringComparer.OrdinalIgnoreCase)).ToList();
            sb.AppendLine(shown.Count == 0
                ? $"{l["none"]} {Tag(c, claims)}"
                : Table([l["c.claim"], l["c.value"]], shown.Select(r => (IReadOnlyList<string>)[Cell(r.Text("ClaimType")), Cell(MaskClaim(r.Text("ClaimType"), r.Text("ClaimValue")))])) + $"\n\n{l["source"]}: {Tag(c, claims)}");
        } else {
            sb.AppendLine(Unavailable(c, claims, l));
        }
        return sb.ToString();
    }

    private string Transaction(RiskCase c, RiskReportLabels l) {
        var sb = new StringBuilder();
        var anchorResult = c.First("Q-RISK-01");
        if (c.AnchorEvent is { } e) {
            sb.AppendLine(Table([l["c.field"], l["c.value"]], [
                [l["f.eventId"], Cell(e.Text("Id"))],
                [l["f.recorded"], Cell(Time(e.Time("CreatedAt")))],
                [l["f.name"], Cell(e.Text("Name"))],
                [l["f.type"], Cell(e.Text("Type"))],
                [l["f.amount"], Cell(e.Text("Amount"))],
                [l["tun"], Cell(e.Text("SourceTransId"))],
                [l["f.ip"], Cell(e.Text("IpAddress"))],
                [l["f.country"], Cell(e.Text("CountryIsoCode"))],
                [l["f.location"], Cell(e.Text("Location"))],
                [l["f.source"], Cell(e.Text("SourceId"))],
                [l["f.session"], Cell(e.Text("SessionId"))]
            ]));
            sb.AppendLine().AppendLine($"{l["source"]}: {Tag(c, anchorResult!)}");

            var data = _redactor.RedactJson(e.Text("Data"));
            if (data is not null) {
                var fields = Flatten(data).ToList();
                if (fields.Count > 0) {
                    sb.AppendLine().AppendLine($"### {l["h.eventData"]}").AppendLine();
                    sb.AppendLine(Table([l["c.field"], l["c.value"]], fields.Select(f => (IReadOnlyList<string>)[Cell(f.Path), Cell(f.Value)])));
                    sb.AppendLine().AppendLine($"{l["source"]}: {Tag(c, anchorResult!)}");
                }
            }
        } else {
            sb.AppendLine(Unavailable(c, anchorResult, l));
        }

        sb.AppendLine().AppendLine($"### {l["h.chain"]}").AppendLine();
        var chain = c.First("Q-RISK-06") ?? c.First("Q-RISK-05");
        sb.AppendLine(Block(c, chain, l, [
            (l["c.time"], r => Time(r.Time("CreatedAt"))),
            (l["c.event"], r => r.Text("Name")),
            (l["c.amount"], r => r.Text("Amount")),
            ("TUN", r => r.Text("SourceTransId")),
            (l["c.level"], r => r.Text("RiskLevel")),
            (l["c.score"], r => r.Text("RiskScore"))
        ], l["none"]));

        var scores = c.Of("Q-RISK-02").ToList();
        if (scores.Count > 0) {
            sb.AppendLine().AppendLine($"### {l["h.scores"]}").AppendLine();
            var stepNames = (chain?.Rows ?? []).Where(r => r.Text("Id") is not null).ToDictionary(r => r.Text("Id")!, r => $"{r.Text("Name")} {Time(r.Time("CreatedAt"))}", StringComparer.OrdinalIgnoreCase);
            foreach (var score in scores) {
                var eventId = score.Parameters.TryGetValue("EventId", out var id) ? id.GetString() : null;
                var label = eventId is not null && stepNames.TryGetValue(eventId, out var name) ? name : eventId ?? string.Empty;
                sb.AppendLine($"**{Escape(label)}**").AppendLine();
                if (score.Succeeded && score.Rows.Count > 0) {
                    var total = score.Rows[0];
                    sb.AppendLine($"{l["c.level"]}: {Cell(total.Text("RiskLevel"))}, {l["c.score"]}: {Cell(total.Text("RiskScore"))}").AppendLine();
                }
                sb.AppendLine(Block(c, score, l, [
                    (l["c.rule"], r => r.Text("ruleName")),
                    (l["c.level"], r => r.Text("RuleLevel")),
                    (l["c.score"], r => r.Text("RuleScore"))
                ], l["none"])).AppendLine();
            }
            sb.AppendLine($"_{l["scoreNotVerdict"]}_");
        }

        sb.AppendLine().AppendLine($"### {l["h.sameDay"]}").AppendLine();
        sb.AppendLine(Block(c, c.First("Q-RISK-03"), l, [
            (l["c.time"], r => Time(r.Time("CreatedAt"))),
            (l["c.event"], r => r.Text("Name")),
            (l["c.amount"], r => r.Text("Amount")),
            (l["c.ip"], r => r.Text("IpAddress")),
            (l["c.level"], r => r.Text("RiskLevel")),
            (l["c.score"], r => r.Text("RiskScore"))
        ], l["none"]));
        return sb.ToString();
    }

    private string Authentication(RiskCase c, RiskReportLabels l) {
        var sb = new StringBuilder();
        var login = c.First("Q-SIGN-02");
        if (login is null) {
            sb.AppendLine(c.AnchorEvent?.Text("SessionId") is null ? l["noSession"] : l["notRun"]);
        } else {
            sb.AppendLine($"### {l["h.login"]}").AppendLine();
            sb.AppendLine(Block(c, login, l, [
                (l["c.time"], r => Time(r.Time("CreatedAt"))),
                (l["c.result"], r => r.Flag("Succeeded") is { } ok ? (ok ? l["c.success"] : l["c.failure"]) : null),
                (l["c.action"], r => r.Text("ActionName")),
                (l["c.application"], r => r.Text("ApplicationId")),
                (l["c.ip"], r => r.Text("IpAddress")),
                (l["c.country"], r => r.Text("CountryIsoCode")),
                (l["c.device"], r => r.Text("DeviceId")),
                (l["c.methods"], r => string.Join(", ", AuthenticationMethods(r))),
                (l["c.registered"], r => r.Json("ExtraData").Property("userDevice").Property("name").AsText())
            ], l["noLogin"]));
        }

        var devices = c.First("Q-ID-04");
        var usedDeviceIds = (login?.Rows ?? []).Select(r => r.Json("ExtraData").Property("userDevice").Property("id").AsText()).Where(id => id is not null).ToHashSet(StringComparer.OrdinalIgnoreCase);
        sb.AppendLine().AppendLine($"### {l["h.devices"]}").AppendLine();
        sb.AppendLine(Block(c, devices, l, [
            (l["c.name"], r => r.Text("Name")),
            (l["c.model"], r => r.Text("Model")),
            (l["c.os"], r => r.Text("OsVersion")),
            (l["c.created"], r => Time(r.Time("DateCreated"))),
            (l["c.lastSignIn"], r => Time(r.Time("LastSignInDate"))),
            (l["c.trusted"], r => YesNo(r.Flag("IsTrusted"), l)),
            (l["c.blocked"], r => YesNo(r.Flag("Blocked"), l)),
            (l["c.id"], r => r.Text("Id")),
            (l["c.usedHere"], r => r.Text("Id") is { } id && usedDeviceIds.Contains(id) ? l["c.yes"] : string.Empty)
        ], l["none"]));

        if (login is { Succeeded: true, Rows.Count: > 0 } && devices is { Succeeded: true }) {
            var matched = usedDeviceIds.Count > 0 && devices.Rows.Any(r => r.Text("Id") is { } id && usedDeviceIds.Contains(id));
            sb.AppendLine().AppendLine(l.Format(matched ? "deviceRegistered" : "deviceNotRegistered", Time(login.Rows[^1].Time("CreatedAt"))) + $" {Tag(c, login)}, {Tag(c, devices)}");
        }
        return sb.ToString();
    }

    private string Notifications(RiskCase c, RiskReportLabels l) {
        var sb = new StringBuilder();
        var notifications = c.First("Q-RISK-08");
        sb.AppendLine($"### {l["h.notifications"]}").AppendLine();
        sb.AppendLine(Block(c, notifications, l, [
            (l["c.time"], r => Time(r.Time("CreatedAt"))),
            (l["c.event"], r => r.Text("Name")),
            (l["c.channel"], r => r.Text("DeliveryChannel")),
            (l["c.recipient"], r => string.Join(" ", new[] { _redactor.MaskPhone(r.Text("PhoneNumber")), _redactor.MaskEmail(r.Text("Email")) }.Where(v => !string.IsNullOrWhiteSpace(v)))),
            (l["c.tracking"], r => r.Text("TrackingId")),
            (l["c.message"], r => RiskEvidenceRedactor.MaskOtp(r.Text("SmsBody") ?? r.Text("EmailBody") ?? r.Text("PushNotificationBody")))
        ], l["none"]));
        if (notifications is { Succeeded: true, Rows.Count: > 0 }) {
            var times = notifications.Rows.Select(r => r.Time("CreatedAt")).Where(t => t is not null).Select(t => t!.Value).ToList();
            if (times.Count > 0) {
                sb.AppendLine().AppendLine(l.Format("otpCount", notifications.Rows.Count, Time(times.Min()), Time(times.Max())) + $" {Tag(c, notifications)}");
            }
        }
        var card = c.First("Q-RISK-09");
        if (card is not null) {
            sb.AppendLine().AppendLine($"### {l["h.sia"]}").AppendLine();
            sb.AppendLine(Block(c, card, l, [
                (l["c.time"], r => Time(r.Time("CreatedAt"))),
                (l["c.ip"], r => r.Text("IpAddress")),
                ("SIA", r => RedactedText(r.Text("SiaResponse")))
            ], l["none"]));
        }
        return sb.ToString();
    }

    private string Network(RiskCase c, RiskReportLabels l) {
        var sb = new StringBuilder();
        var ips = c.First("Q-RISK-19");
        sb.AppendLine($"### {l["h.usualIps"]}").AppendLine();
        sb.AppendLine(Block(c, ips, l, [
            (l["c.ip"], r => r.Text("IpAddress")),
            (l["c.country"], r => r.Text("CountryIsoCode")),
            (l["c.location"], r => r.Text("Location")),
            (l["c.count"], r => r.Text("Events")),
            (l["c.first"], r => Time(r.Time("FirstSeen"))),
            (l["c.last"], r => Time(r.Time("LastSeen")))
        ], l["none"]));

        var logins = c.First("Q-SIGN-06");
        sb.AppendLine().AppendLine($"### {l["h.usualLogins"]}").AppendLine();
        sb.AppendLine(Block(c, logins, l, [
            (l["c.country"], r => r.Text("CountryIsoCode")),
            (l["c.location"], r => r.Text("Location")),
            (l["c.ip"], r => r.Text("IpAddress")),
            (l["c.count"], r => r.Text("Logins")),
            (l["c.first"], r => Time(r.Time("FirstSeen"))),
            (l["c.last"], r => Time(r.Time("LastSeen")))
        ], l["none"]));

        var anchor = c.AnchorEvent;
        var ip = anchor?.Text("IpAddress");
        if (ip is not null && ips is { Succeeded: true }) {
            sb.AppendLine();
            var seen = ips.Rows.FirstOrDefault(r => string.Equals(r.Text("IpAddress"), ip, StringComparison.OrdinalIgnoreCase));
            sb.AppendLine((seen is null
                ? l.Format("ipNew", ip, _usualPatternDays)
                : l.Format("ipSeen", ip, _usualPatternDays, seen.Text("Events"), Time(seen.Time("FirstSeen")), Time(seen.Time("LastSeen")))) + $" {Tag(c, ips)}");
            var country = anchor!.Text("CountryIsoCode");
            if (!string.IsNullOrWhiteSpace(country)) {
                var countrySeen = ips.Rows.Any(r => string.Equals(r.Text("CountryIsoCode"), country, StringComparison.OrdinalIgnoreCase));
                sb.AppendLine().AppendLine($"{l.Format(countrySeen ? "countrySeen" : "countryNew", country)} {Tag(c, ips)}");
            }
        }
        return sb.ToString();
    }

    private string Changes(RiskCase c, RiskReportLabels l) {
        var sb = new StringBuilder();
        var riskChanges = c.First("Q-RISK-10");
        sb.AppendLine($"### {l["h.riskChanges"]}").AppendLine();
        sb.AppendLine(Block(c, riskChanges, l, [
            (l["c.time"], r => Time(r.Time("CreatedAt"))),
            (l["c.event"], r => r.Text("Name")),
            (l["c.source"], r => r.Text("SourceId")),
            (l["c.ip"], r => r.Text("IpAddress")),
            (l["c.session"], r => r.Text("SessionId"))
        ], l["none"]));

        var activity = c.First("Q-ACT-01");
        sb.AppendLine().AppendLine($"### {l["h.activity"]}").AppendLine();
        sb.AppendLine(Block(c, activity, l, [
            (l["c.time"], r => Time(r.Time("CreatedAt"))),
            (l["c.category"], r => r.Text("Category")),
            (l["c.event"], r => r.Text("EventType")),
            (l["c.actor"], r => r.Text("ActorName") ?? r.Text("ActorId")),
            (l["c.target"], r => string.Join(" ", new[] { r.Text("ResourceType"), r.Text("ResourceId") }.Where(v => !string.IsNullOrWhiteSpace(v)))),
            (l["c.ip"], r => r.Text("IpAddress")),
            (l["c.session"], r => r.Text("SessionId"))
        ], l["none"]));

        if (riskChanges is { Succeeded: true } && activity is { Succeeded: true }) {
            sb.AppendLine().AppendLine(l.Format("changesFound", riskChanges.Rows.Count, activity.Rows.Count, _priorChangesDays) + $" {Tag(c, riskChanges)}, {Tag(c, activity)}");
        }
        return sb.ToString();
    }

    private string Coverage(RiskCase c, RiskReportLabels l) {
        var sb = new StringBuilder();
        sb.AppendLine($"### {l["h.riskCoverage"]}").AppendLine();
        sb.AppendLine(Block(c, c.First("Q-RISK-17"), l, [
            (l["c.source"], r => r.Text("SourceId")),
            (l["c.count"], r => r.Text("Events")),
            (l["c.first"], r => Time(r.Time("FirstEvent"))),
            (l["c.last"], r => Time(r.Time("LastEvent")))
        ], l["none"]));

        var sign = c.First("Q-SIGN-07");
        sb.AppendLine().AppendLine($"### {l["h.signCoverage"]}").AppendLine();
        sb.AppendLine(Block(c, sign, l, [
            (l["c.table"], r => r.Text("TableName")),
            (l["c.oldest"], r => Time(r.Time("Oldest"))),
            (l["c.newest"], r => Time(r.Time("Newest")))
        ], l["none"]));
        var loginTime = c.First("Q-SIGN-02") is { Succeeded: true, Rows.Count: > 0 } loginResult ? loginResult.Rows.Select(r => r.Time("CreatedAt")).Where(t => t is not null).Min() : null;
        if (sign is { Succeeded: true } && loginTime is { } at) {
            var oldest = sign.Rows.Select(r => r.Time("Oldest")).Where(t => t is not null).DefaultIfEmpty(null).Min();
            if (oldest is { } start) {
                sb.AppendLine().AppendLine(l.Format("signInCoverage", Time(at), l[at >= start ? "within" : "before"], Time(start)) + $" {Tag(c, sign)}");
            }
        }

        sb.AppendLine().AppendLine($"### {l["h.activityCoverage"]}").AppendLine();
        sb.AppendLine(Block(c, c.First("Q-ACT-05"), l, [
            (l["c.oldest"], r => Time(r.Time("Oldest"))),
            (l["c.newest"], r => Time(r.Time("Newest")))
        ], l["none"]));
        return sb.ToString();
    }

    private string Limitations(RiskCase c, RiskReportContext context, RiskReportLabels l) {
        var sb = new StringBuilder();
        sb.AppendLine($"### {l["reportLimitations"]}").AppendLine();
        var failed = c.Results.Count(r => !r.Succeeded);
        var truncated = c.Results.Count(r => r.Truncated);
        var items = new List<string> { l["masked"], l["maskedOtp"], l["scoreNotVerdict"], l["notVerdict"] };
        if (failed > 0) items.Add(l.Format("failedQueries", failed));
        if (truncated > 0) items.Add(l.Format("truncatedQueries", truncated));
        foreach (var item in items) {
            sb.AppendLine($"- {Escape(item)}");
        }

        sb.AppendLine().AppendLine($"### {l["limitationText"]}").AppendLine();
        foreach (var id in LimitationIds) {
            var snippet = context.Limitations.FirstOrDefault(s => s.HeadingPath.Split(" > ").Any(segment => segment.StartsWith(id + " ", StringComparison.OrdinalIgnoreCase)));
            if (snippet is null) {
                sb.AppendLine($"- **{id}**: {l["limitationMissing"]}");
                continue;
            }
            var heading = snippet.HeadingPath.Split(" > ").Last();
            sb.AppendLine($"- **{Escape(heading)}**: {Escape(Whitespace().Replace(snippet.Text, " ").Trim())}");
        }
        return sb.ToString();
    }

    private string Annex(RiskCase c, RiskReportLabels l) =>
        Table([ "E", l["c.query"], l["c.database"], l["c.executed"], l["c.parameters"], l["c.rows"], l["c.status"], l["c.sha"] ],
              c.Results.Select((r, index) => (IReadOnlyList<string>)[
                  $"E-{index + 1}",
                  Cell(r.QueryId),
                  Cell(r.Database),
                  Cell(r.ExecutedAt == default ? null : r.ExecutedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                  Cell(string.Join(", ", r.Parameters.Select(p => $"{p.Key}={ParameterValue(p.Value)}"))),
                  Cell(r.RowCount.ToString(CultureInfo.InvariantCulture)),
                  Cell(!r.Succeeded ? $"{l["c.failed"]}: {r.Error}" : r.Truncated ? l["c.truncatedShort"] : l["c.ok"]),
                  Cell(r.SqlSha256)
              ]));

    // ---- building blocks --------------------------------------------------------------------------------------------------

    private static void AppendSection(StringBuilder sb, int number, string heading, string body) =>
        sb.AppendLine($"## {number}. {Escape(heading)}").AppendLine().AppendLine(body.TrimEnd()).AppendLine();

    private static string Tag(RiskCase c, RiskQueryResult result) => $"[E-{c.NumberOf(result)}]";

    private string Unavailable(RiskCase c, RiskQueryResult? result, RiskReportLabels l) =>
        result is null
            ? $"> {l["notRun"]}"
            : $"> ⚠ {l["incomplete"]} {Tag(c, result)} {result.QueryId}: {Escape(result.Error ?? string.Empty)}";

    /// <summary>A result as a table, or the reason there is none: not run, failed, or empty. A truncated result carries a warning.</summary>
    private string Block(RiskCase c, RiskQueryResult? result, RiskReportLabels l, (string Header, Func<Dictionary<string, JsonElement>, string?> Value)[] columns, string empty) {
        if (result is null || !result.Succeeded) {
            return Unavailable(c, result, l);
        }
        if (result.Rows.Count == 0) {
            return $"{empty} {Tag(c, result)}";
        }
        var sb = new StringBuilder();
        sb.AppendLine(Table(columns.Select(col => col.Header).ToList(), result.Rows.Select(row => (IReadOnlyList<string>)[.. columns.Select(col => Cell(col.Value(row)))])));
        if (result.Truncated) {
            sb.AppendLine().AppendLine($"> ⚠ {l["truncated"]}");
        }
        sb.AppendLine().Append($"{l["source"]}: {Tag(c, result)}");
        return sb.ToString();
    }

    private static string Table(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows) {
        var sb = new StringBuilder();
        sb.Append("| ").Append(string.Join(" | ", headers.Select(Escape))).AppendLine(" |");
        sb.Append('|').Append(string.Concat(Enumerable.Repeat(" --- |", headers.Count))).AppendLine();
        foreach (var row in rows) {
            sb.Append("| ").Append(string.Join(" | ", row)).AppendLine(" |");
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>A table cell: escaped so that log content cannot become markup, collapsed to one line, and cut at a sensible length.</summary>
    internal static string Cell(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "—";
        }
        var line = Whitespace().Replace(value, " ").Trim();
        if (line.Length > MaxCellLength) {
            line = line[..MaxCellLength] + "…";
        }
        return Escape(line);
    }

    /// <summary>Escapes the characters markdown treats as markup, and the angle brackets that would start HTML.</summary>
    internal static string Escape(string? value) {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }
        var sb = new StringBuilder(value.Length + 8);
        foreach (var ch in value) {
            if (ch is '\\' or '`' or '*' or '_' or '[' or ']' or '<' or '>' or '|' or '#') {
                sb.Append('\\');
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>A UTC instant with its local equivalent: <c>2026-11-12 09:30:15 UTC (11:30:15 UTC+02:00)</c>.</summary>
    internal string? Time(DateTimeOffset? instant) {
        if (instant is not { } value) {
            return null;
        }
        var utc = value.ToUniversalTime();
        var offset = _zone.GetUtcOffset(utc);
        var local = utc.ToOffset(offset);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        return string.Create(CultureInfo.InvariantCulture, $"{utc:yyyy-MM-dd HH:mm:ss} UTC ({local:HH:mm:ss} UTC{sign}{offset:hh\\:mm})");
    }

    private static string? YesNo(bool? value, RiskReportLabels l) => value is null ? null : value.Value ? l["c.yes"] : l["c.no"];

    private string? MaskClaim(string? type, string? value) => type?.ToLowerInvariant() switch {
        "tin" => _redactor.MaskIdentifier(value),
        "phone1" or "phone2" => _redactor.MaskPhone(value),
        _ => value
    };

    private static IEnumerable<string> AuthenticationMethods(Dictionary<string, JsonElement> row) =>
        row.Json("ExtraData").Property("authenticationMethods") is { ValueKind: JsonValueKind.Array } methods
            ? methods.EnumerateArray().Select(m => m.GetString() ?? string.Empty).Where(m => m.Length > 0)
            : [];

    private static string ParameterValue(JsonElement value) => value.ValueKind switch {
        JsonValueKind.Null => "null",
        JsonValueKind.String => value.GetString() ?? string.Empty,
        _ => value.ToString()
    };

    private string? RedactedText(string? json) => _redactor.RedactJson(json)?.ToJsonString() ?? RiskEvidenceRedactor.MaskOtp(json);

    /// <summary>Flattens a JSON value into <c>path: value</c> pairs, three levels deep and at most forty fields, so a large payload cannot flood the report.</summary>
    internal static IEnumerable<(string Path, string Value)> Flatten(JsonNode node, string path = "", int depth = 0) {
        var emitted = 0;
        foreach (var field in FlattenCore(node, path, depth)) {
            if (++emitted > 40) {
                yield break;
            }
            yield return field;
        }
    }

    private static IEnumerable<(string Path, string Value)> FlattenCore(JsonNode? node, string path, int depth) {
        switch (node) {
            case JsonObject obj when depth < 3:
                foreach (var (name, child) in obj) {
                    foreach (var field in FlattenCore(child, path.Length == 0 ? name : $"{path}.{name}", depth + 1)) {
                        yield return field;
                    }
                }
                break;
            case JsonArray array when depth < 3:
                for (var i = 0; i < array.Count; i++) {
                    foreach (var field in FlattenCore(array[i], $"{path}[{i}]", depth + 1)) {
                        yield return field;
                    }
                }
                break;
            case null:
                break;
            case JsonValue value:
                yield return (path, value.ToString());
                break;
            default:
                yield return (path, node.ToJsonString());
                break;
        }
    }
}
