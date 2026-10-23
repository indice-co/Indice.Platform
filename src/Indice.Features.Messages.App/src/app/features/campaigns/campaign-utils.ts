import { ChannelKind } from 'src/app/ui/badges/badges';

/** Call to action link validation, as used by the API era UI. */
export const URL_PATTERN = /https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)(\/?)*$/;

/** Formats a date for a `datetime-local` input (`yyyy-MM-ddTHH:mm`, local time). */
export function toLocalInput(value: Date | string | null | undefined): string {
  if (!value) {
    return '';
  }
  const date = new Date(value);
  if (isNaN(date.getTime())) {
    return '';
  }
  const pad = (n: number) => `${n}`.padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export interface ContentChannel {
  /** Key used in the campaign / template `content` dictionary. */
  key: string;
  kind: ChannelKind;
  name: string;
  description: string;
}

/** Channels a campaign can carry content for, keyed as in the `content` dictionary. */
export const CONTENT_CHANNELS: ContentChannel[] = [
  { key: 'inbox', kind: 'Inbox', name: 'Campaigns.ChannelInbox', description: 'Campaigns.ChannelInboxDescription' },
  { key: 'pushNotification', kind: 'PushNotification', name: 'Campaigns.ChannelPushNotification', description: 'Campaigns.ChannelPushNotificationDescription' },
  { key: 'email', kind: 'Email', name: 'Campaigns.ChannelEmail', description: 'Campaigns.ChannelEmailDescription' },
  { key: 'sms', kind: 'SMS', name: 'Campaigns.ChannelSms', description: 'Campaigns.ChannelSmsDescription' }
];

/** Resolves a `content` dictionary key (any casing) to its channel. */
export function contentChannel(key: string | undefined): ContentChannel | undefined {
  const lower = (key ?? '').toLowerCase();
  return CONTENT_CHANNELS.find(c => c.key.toLowerCase() === lower || c.kind.toLowerCase() === lower);
}
