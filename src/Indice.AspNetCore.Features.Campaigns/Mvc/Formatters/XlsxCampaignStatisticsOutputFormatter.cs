using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Extensions;
using Indice.Security;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace Indice.AspNetCore.Features.Campaigns.Formatters
{
    internal class XlsxCampaignStatisticsOutputFormatter : OutputFormatter
    {
        private DateTime _utcNow;

        public XlsxCampaignStatisticsOutputFormatter() {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(FileExtensions.GetMimeType("xlsx")));
        }

        protected override bool CanWriteType(Type type) => typeof(CampaignStatistics).IsAssignableFrom(type) && base.CanWriteType(type);

        public override void WriteResponseHeaders(OutputFormatterWriteContext context) {
            var data = (CampaignStatistics)context.Object;
            var response = context.HttpContext.Response;
            response.ContentType = FileExtensions.GetMimeType("xlsx");
            var contentDisposition = new ContentDispositionHeaderValue("attachment");
            _utcNow = DateTime.UtcNow;
            contentDisposition.SetHttpFileName($"{data.Title.ToLower()}_{_utcNow:yyyyMMddHHmm}.xlsx");
            response.Headers.Add(HeaderNames.ContentDisposition, contentDisposition.ToString());
        }

        public async override Task WriteResponseBodyAsync(OutputFormatterWriteContext context) {
            var data = (CampaignStatistics)context.Object;
            var httpContext = context.HttpContext;
            var user = httpContext.User;
            using (var xlPackage = new ExcelPackage()) {
                var worksheet = xlPackage.Workbook.Worksheets.Add(data.Title);
                var headerText = data.Title;
                var headerDate = _utcNow.ToString("dd/MM/yyyy hh:mm");
                // Header text at center, for every page.
                worksheet.HeaderFooter.FirstHeader.CenteredText = headerText;
                worksheet.HeaderFooter.EvenHeader.CenteredText = headerText;
                worksheet.HeaderFooter.OddHeader.CenteredText = headerText;
                // Header date at right, for every page.
                worksheet.HeaderFooter.FirstHeader.RightAlignedText = headerDate;
                worksheet.HeaderFooter.EvenHeader.RightAlignedText = headerDate;
                worksheet.HeaderFooter.OddHeader.RightAlignedText = headerDate;
                // Page number at right, for all pages.
                worksheet.HeaderFooter.FirstFooter.RightAlignedText = $"Page {ExcelHeaderFooter.PageNumber} of {ExcelHeaderFooter.NumberOfPages}";
                worksheet.HeaderFooter.EvenFooter.RightAlignedText = $"Page {ExcelHeaderFooter.PageNumber} of {ExcelHeaderFooter.NumberOfPages}";
                worksheet.HeaderFooter.OddFooter.RightAlignedText = $"Page {ExcelHeaderFooter.PageNumber} of {ExcelHeaderFooter.NumberOfPages}";
                // User text at left, for all pages.
                var userText = $"This document was requested by '{user.FindDisplayName()}'.";
                worksheet.HeaderFooter.FirstFooter.LeftAlignedText = userText;
                worksheet.HeaderFooter.EvenFooter.LeftAlignedText = userText;
                worksheet.HeaderFooter.OddFooter.LeftAlignedText = userText;
                // Create columns.
                worksheet.Cells[1, 1].Value = "Read Count";
                worksheet.Cells[1, 2].Value = "Not Read Count";
                worksheet.Cells[1, 3].Value = "Deleted Count";
                worksheet.Cells[1, 4].Value = "CTA Count";
                // Write columns.
                worksheet.Cells[2, 1].Value = data.ReadCount;
                worksheet.Cells[2, 2].Value = data.NotReadCount.HasValue ? data.NotReadCount.ToString() : "N/A";
                worksheet.Cells[2, 3].Value = data.DeletedCount;
                worksheet.Cells[2, 4].Value = data.ClickToActionCount;
                // Create table name and apply some settings.
                var tableName = Regex.Replace(data.Title.Unidecode(), @"[^0-9a-zA-Z ]+", string.Empty).ToLowerInvariant();
                tableName = Regex.Replace(tableName, @"\s+(\w)", match => match.Groups[1].Value.ToUpperInvariant(), RegexOptions.IgnoreCase).Trim(); // We need to remove whitespaces.
                var table = worksheet.Tables.Add(worksheet.Cells[1, 1, 2, 4], tableName);
                table.TableStyle = TableStyles.Medium27;
                worksheet.View.ShowGridLines = false;
                worksheet.Calculate();
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                // Protect worksheet.
                worksheet.Protection.SetPassword($"{Guid.NewGuid()}");
                // Write excel data to output stream.
                var httpBodyControl = httpContext.Features.Get<IHttpBodyControlFeature>();
                httpBodyControl.AllowSynchronousIO = true;
                xlPackage.SaveAs(context.HttpContext.Response.Body);
                await Task.CompletedTask;
            }
        }
    }
}
