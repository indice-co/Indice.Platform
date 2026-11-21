using System.Net;
using Markdig;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>
/// Renders the report markdown as one self-contained, print-ready HTML page: A4 page rules, no scripts, no external resources.
/// Raw HTML in the markdown is escaped, not emitted: the report holds text that came from logs (message bodies, device names), and
/// that text must never become markup.
/// </summary>
internal static class RiskReportHtml
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .DisableHtml()
        .Build();

    private const string Style = """
        @page { size: A4; margin: 16mm 14mm; }
        * { box-sizing: border-box; }
        body { font-family: "Segoe UI", Roboto, Arial, "Noto Sans", sans-serif; font-size: 10.5pt; line-height: 1.45; color: #1a1a1a; margin: 0 auto; max-width: 190mm; padding: 8mm; }
        h1 { font-size: 18pt; margin: 0 0 6mm; }
        h2 { font-size: 13pt; margin: 8mm 0 3mm; padding-bottom: 1mm; border-bottom: 1px solid #999; break-after: avoid; }
        h3 { font-size: 11pt; margin: 5mm 0 2mm; break-after: avoid; }
        table { border-collapse: collapse; width: 100%; margin: 2mm 0; font-size: 8.5pt; }
        th, td { border: 1px solid #b5b5b5; padding: 1mm 1.6mm; text-align: left; vertical-align: top; overflow-wrap: anywhere; }
        th { background: #efefef; }
        tr { break-inside: avoid; }
        blockquote { margin: 2mm 0; padding: 1mm 3mm; border-left: 3px solid #c08a00; background: #fff8e6; }
        footer { margin-top: 10mm; padding-top: 2mm; border-top: 1px solid #999; font-size: 8pt; color: #444; overflow-wrap: anywhere; }
        @media print { body { padding: 0; max-width: none; } }
        """;

    /// <summary>Renders the page.</summary>
    /// <param name="markdown">The report markdown.</param>
    /// <param name="title">The document title.</param>
    /// <param name="language">The page language, <c>el</c> or <c>en</c>.</param>
    /// <param name="reportId">The report id, repeated in the footer.</param>
    /// <param name="markdownSha256">The SHA-256 of the markdown file, so the printed page can be tied to the file.</param>
    /// <param name="footerLabel">The text that introduces the identifiers in the footer.</param>
    public static string Render(string markdown, string title, string language, string reportId, string markdownSha256, string footerLabel) {
        var body = Markdown.ToHtml(markdown, Pipeline);
        return $$"""
            <!doctype html>
            <html lang="{{WebUtility.HtmlEncode(language)}}">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'">
            <meta name="robots" content="noindex">
            <title>{{WebUtility.HtmlEncode(title)}}</title>
            <style>
            {{Style}}
            </style>
            </head>
            <body>
            {{body}}
            <footer>{{WebUtility.HtmlEncode(footerLabel)}} {{WebUtility.HtmlEncode(reportId)}} · SHA-256 (Markdown): {{WebUtility.HtmlEncode(markdownSha256)}}</footer>
            </body>
            </html>
            """;
    }
}
