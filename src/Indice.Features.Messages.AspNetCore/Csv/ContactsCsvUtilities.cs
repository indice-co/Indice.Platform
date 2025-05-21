using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Indice.Features.Messages.AspNetCore.Csv.Records;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.AspNetCore.Csv;

internal class ContactsCsvUtilities
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

    internal static async Task<List<CreateDistributionListContactRequest>> Import(Stream fileStream, CancellationToken cancellationToken = default) {
        var contactRequests = new List<CreateDistributionListContactRequest>();
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) {
            Encoding = Encoding.UTF8,
            DetectDelimiter = true,
            ShouldQuote = args => true
        };
        using var reader = new StreamReader(fileStream);
        using var csvReader = new CsvReader(reader, csvConfig);
        await foreach (var record in csvReader.GetRecordsAsync<ContactCsvRecord>().WithCancellation(cancellationToken)) {
            contactRequests.Add(record.ToCreateDistributionListContactRequest());
        }
        return contactRequests;
    }
}
