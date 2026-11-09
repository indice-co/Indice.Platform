using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="MergeContactsEvent"/>.</summary>
public class MergeContactsEventHandler : ICampaignJobHandler<MergeContactsEvent>
{
    /// <summary>Creates a new instance of <see cref="MergeContactsEventHandler"/>.</summary>
    /// <param name="contactService">Contacts management service</param>
    /// <param name="logger">Logging service</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MergeContactsEventHandler(IContactService contactService, ILogger<MergeContactsEventHandler> logger) {
        _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private readonly IContactService _contactService;
    private readonly ILogger<MergeContactsEventHandler> _logger;

    /// <summary>Merges duplicate contacts into a primary contact.</summary>
    /// <param name="event">The event model used when merging contacts.</param>
    public async Task Process(MergeContactsEvent @event) {
        var mainContact = await _contactService.GetById(@event.PrimaryContactId);
        if (mainContact is null) {
            _logger.LogError("No Contact was found with the given Id: {PrimaryContactId}", @event.PrimaryContactId);
            return;
        }
        if (string.IsNullOrWhiteSpace(mainContact.RecipientId)) {
            _logger.LogError("The main contact does not have a recipient Id: {PrimaryContactId}", @event.PrimaryContactId);
            return;
        }
        if (@event.DuplicateContactsIds is null || @event.DuplicateContactsIds.Count == 0) {
            _logger.LogError("Duplicates list cannot be empty for primary contact Id: {PrimaryContactId}", @event.PrimaryContactId);
            return;
        }
        if (@event.DuplicateContactsIds.Contains(@event.PrimaryContactId)) {
            _logger.LogError("Duplicates list should not contain main contact Id: {PrimaryContactId}", @event.PrimaryContactId);
            return;
        }
        await _contactService.MergeContacts(mainContact, @event.DuplicateContactsIds);
    }
}