using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;

namespace Indice.Features.Messages.Tests.Mocks;
internal class MockContactResolver : IContactResolver
{
    private List<ContactPreferences> _contacts = new() { 
        new ContactPreferences() {
            RecipientId = "6c9fa6dd-ede4-486b-bf91-6de18542da4a",
            FirstName = "Indice",
            LastName = "User",
            FullName = "Indice User",
            Email = "company@indice.gr",
            PhoneNumber = "1234567890",
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
        }
    };
    public Task<ResultSet<ContactPreferences>> Find(ListOptions options) {
        return Task.FromResult(_contacts.ToResultSet());
    }

    public Task<ContactPreferences> Resolve(string recipientId) {
        return Task.FromResult(_contacts.FirstOrDefault(i => i.RecipientId == recipientId));
    }
}
