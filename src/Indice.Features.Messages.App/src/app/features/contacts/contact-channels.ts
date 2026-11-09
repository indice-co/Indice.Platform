import { Contact } from 'src/app/core/services/messages-api.service';
import { ChannelKind } from 'src/app/ui/badges/badges';

/** Channels a contact can be reached on, judged by the identifiers it has. */
export function contactChannels(contact: Contact): { kind: ChannelKind; available: boolean }[] {
  return [
    { kind: 'Email', available: !!contact.email },
    { kind: 'SMS', available: !!contact.phoneNumber },
    { kind: 'PushNotification', available: !!contact.recipientId },
    { kind: 'Inbox', available: !!contact.recipientId }
  ];
}
