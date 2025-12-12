using CsvHelper.Configuration.Attributes;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.AspNetCore.Csv.Records;
internal record ContactCsvRecord(
    [Optional] string? RecipientId,
    [Optional] string? Salutation,
    string? FirstName,
    string? LastName,
    [Optional] string? FullName,
    string? Email,
    [Optional] string? PhoneNumber
)
{
    public CreateDistributionListContactRequest ToCreateDistributionListContactRequest() {
        return new CreateDistributionListContactRequest {
            RecipientId = string.IsNullOrWhiteSpace(RecipientId) ? null : RecipientId,
            Salutation = string.IsNullOrWhiteSpace(Salutation) ? null : Salutation,
            FirstName = string.IsNullOrWhiteSpace(FirstName) ? null : FirstName,
            LastName = string.IsNullOrWhiteSpace(LastName) ? null : LastName,
            FullName = !string.IsNullOrWhiteSpace(FullName) ? FullName : !string.IsNullOrWhiteSpace($"{FirstName} {LastName}".Trim()) ? $"{FirstName} {LastName}".Trim() : null,
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email,
            PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber
        };
    }

    public static ContactCsvRecord FromDbContact(Contact contact) {
        return new ContactCsvRecord(
            contact.RecipientId,
            contact.Salutation,
            contact.FirstName,
            contact.LastName,
            contact.FullName,
            contact.Email,
            contact.PhoneNumber
        );
    }
}
