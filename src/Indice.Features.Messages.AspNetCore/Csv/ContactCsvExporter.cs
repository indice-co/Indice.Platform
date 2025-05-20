using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Indice.Features.Messages.AspNetCore.Csv.Records;
using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.AspNetCore.Csv;

internal class ContactCsvExporter
{
    internal static async Task<byte[]> Export(IEnumerable<Contact> contacts) {
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) {
            Encoding = Encoding.UTF8,
            DetectDelimiter = true,
            ShouldQuote = args => true
        };

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
        using var csv = new CsvWriter(writer, csvConfig);

        var exportRecords = contacts.Select(ContactCsvRecord.FromDbContact);
        await csv.WriteRecordsAsync(exportRecords);
        await writer.FlushAsync();

        memoryStream.Position = 0;
        return memoryStream.ToArray();
    }
}
