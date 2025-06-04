using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.AspNetCore.Csv.Records;
internal record ContactCsvRecord(
    string? RecipientId,
    string? Salutation,
    string? FirstName,
    string? LastName,
    string? FullName,
    string? Email,
    string? PhoneNumber,
    string? Locale
)
{
    public CreateDistributionListContactRequest ToCreateDistributionListContactRequest() {
        return new CreateDistributionListContactRequest {
            RecipientId = string.IsNullOrWhiteSpace(RecipientId) ? null : RecipientId,
            Salutation = Salutation,
            FirstName = FirstName,
            LastName = LastName,
            FullName = FullName,
            Email = Email,
            PhoneNumber = PhoneNumber,
            Locale = Locale
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
            contact.PhoneNumber,
            contact.Locale
        );
    }
}
