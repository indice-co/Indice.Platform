import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { MESSAGES_API_BASE_URL } from 'src/app/core/services/messages-api.service';
import { InboxMessage, InboxMessageType, InboxMessagesQuery, InboxResultSet } from './inbox.models';

/** Requests carrying this header do not reveal the global (ngx-progressbar) progress bar; the header is removed before sending. */
const SILENT_HEADERS = new HttpHeaders({ ignoreProgressBar: 'true' });

/** Thin client over the user present "my messages" (inbox) endpoints of the messaging api. */
@Injectable({ providedIn: 'root' })
export class InboxApiService {
  private readonly _http = inject(HttpClient);
  private readonly _baseUrl = (inject(MESSAGES_API_BASE_URL, { optional: true }) ?? '').replace(/\/$/u, '');

  /**
   * Gets the messages of the current user.
   * @param silent When true the request does not reveal the global progress bar (background requests).
   */
  public getMessages(query: InboxMessagesQuery = {}, silent = false): Observable<InboxResultSet<InboxMessage>> {
    let params = new HttpParams();
    const set = (name: string, value: unknown) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.append(name, `${value}`);
      }
    };
    set('Page', query.page);
    set('Size', query.size);
    set('Sort', query.sort);
    set('Search', query.search);
    set('IsRead', query.isRead);
    set('ShowExpired', query.showExpired);
    query.typeId?.forEach(id => set('TypeId', id));
    return this._http
      .get<InboxResultSet<InboxMessage>>(`${this._baseUrl}/my/messages`, { params, headers: silent ? SILENT_HEADERS : undefined })
      .pipe(map(result => ({ count: result?.count ?? 0, items: result?.items ?? [] })));
  }

  /** Gets only the number of unread messages. A page size of zero makes the server return the count without any items. */
  public getUnreadCount(): Observable<number> {
    return this.getMessages({ page: 1, size: 0, isRead: false }, true).pipe(map(result => result.count));
  }

  /** Gets the latest unread messages (along with the total unread count). */
  public getLatestUnread(size: number): Observable<InboxResultSet<InboxMessage>> {
    return this.getMessages({ page: 1, size, sort: 'createdAt-', isRead: false }, true);
  }

  public getMessage(messageId: string): Observable<InboxMessage> {
    return this._http.get<InboxMessage>(`${this._baseUrl}/my/messages/${encodeURIComponent(messageId)}`);
  }

  public markAsRead(messageId: string): Observable<void> {
    return this._http.put<void>(`${this._baseUrl}/my/messages/${encodeURIComponent(messageId)}/read`, null);
  }

  public markAsUnread(messageId: string): Observable<void> {
    return this._http.put<void>(`${this._baseUrl}/my/messages/${encodeURIComponent(messageId)}/unread`, null);
  }

  /** Marks all messages as read. The operation is processed asynchronously by the server. */
  public markAllAsRead(): Observable<void> {
    return this._http.put<void>(`${this._baseUrl}/my/messages/all/read`, {});
  }

  public deleteMessage(messageId: string): Observable<void> {
    return this._http.delete<void>(`${this._baseUrl}/my/messages/${encodeURIComponent(messageId)}`);
  }

  public getMessageTypes(): Observable<InboxMessageType[]> {
    return this._http
      .get<InboxResultSet<InboxMessageType>>(`${this._baseUrl}/messages/types`, { params: { Page: 1, Size: 100, Sort: 'name' } })
      .pipe(map(result => result?.items ?? []));
  }

  /** Resolves the (relative) call to action link of a message against the api address. */
  public resolveActionLink(message: InboxMessage): string | undefined {
    const href = message.actionLink?.href;
    if (!href) {
      return undefined;
    }
    return /^https?:\/\//iu.test(href) ? href : `${this._baseUrl}/${href.replace(/^\//u, '')}`;
  }

  /** Resolves the (host relative) attachment url of a message against the api address. */
  public resolveAttachmentUrl(message: InboxMessage): string | undefined {
    const url = message.attachmentUrl;
    if (!url) {
      return undefined;
    }
    try {
      return new URL(url, this._baseUrl || window.location.origin).href;
    } catch {
      return url;
    }
  }
}
