using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Indice.Features.Messages.AspNetCore.Csv.Records;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.AspNetCore.Csv;

internal static class ContactCsvImporter
{
    internal static async Task<List<CreateDistributionListContactRequest>> Parse(Stream fileStream, CancellationToken cancellationToken = default) {
        var contactRequests = new List<CreateDistributionListContactRequest>();
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) {
            Encoding = Encoding.UTF8,
            DetectDelimiter = true
        };
        using var reader = new StreamReader(fileStream);
        using var csvReader = new CsvReader(reader, csvConfig);
        await foreach (var record in csvReader.GetRecordsAsync<ContactCsvRecord>().WithCancellation(cancellationToken)) {
            contactRequests.Add(record.ToCreateDistributionListContactRequest());
        }
        return contactRequests;
    }
}
