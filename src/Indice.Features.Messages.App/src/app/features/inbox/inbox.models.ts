/**
 * Models of the "my messages" (inbox) api. They mirror the server side models of the `my/messages` endpoints
 * (`Indice.Features.Messages.Core.Models.Message` etc.) and are intentionally independent of the generated management api client,
 * so that the inbox feature can be extracted and reused by other SPAs against the same backend.
 */

export interface InboxHyperlink {
  href?: string | null;
  text?: string | null;
}

export interface InboxPeriod {
  from?: string | null;
  to?: string | null;
}

export interface InboxMessageType {
  id: string;
  name?: string | null;
  alias?: string | null;
  classification?: string | null;
}

export interface InboxMessage {
  /** The id of the message (used by all `my/messages/{messageId}` endpoints). */
  id: string;
  sender?: string | null;
  title?: string | null;
  content?: string | null;
  isRead?: boolean | null;
  actionLink?: InboxHyperlink | null;
  attachmentUrl?: string | null;
  createdAt: string;
  activePeriod?: InboxPeriod | null;
  type?: InboxMessageType | null;
}

export interface InboxResultSet<T> {
  count: number;
  items: T[];
}

export interface InboxMessagesQuery {
  page?: number;
  size?: number;
  /** Server sort expression, i.e. `createdAt-`. */
  sort?: string;
  search?: string;
  typeId?: string[];
  isRead?: boolean;
  showExpired?: boolean;
}

/** Payload pushed by the server (SignalR) whenever a new inbox message is created. */
export interface InboxMessageNotification {
  id: string;
  title?: string | null;
  type?: InboxMessageType | null;
  activePeriod?: InboxPeriod | null;
  isGlobal?: boolean;
  createdAt?: string;
}
