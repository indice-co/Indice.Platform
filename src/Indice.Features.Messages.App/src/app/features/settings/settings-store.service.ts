import { Injectable, inject } from '@angular/core';
import { Observable, map, shareReplay, tap } from 'rxjs';
import { MediaApiClient, MediaSetting, UpdateMediaSettingRequest } from 'src/app/core/services/media-api.service';
import { CreateMessageSenderRequest, MessageSender, MessageSenderResultSet, MessagesApiClient, UpdateMessageSenderRequest } from 'src/app/core/services/messages-api.service';

/** Cached message senders and media settings; every change through the store invalidates the cache. */
@Injectable({ providedIn: 'root' })
export class SettingsStore {
  private readonly _messagesApi = inject(MessagesApiClient);
  private readonly _mediaApi = inject(MediaApiClient);
  private _messageSenders$: Observable<MessageSenderResultSet> | undefined;
  private _mediaSettings$: Observable<MediaSetting[]> | undefined;

  public getMessageSenders(): Observable<MessageSenderResultSet> {
    this._messageSenders$ ??= this._messagesApi.getMessageSenders().pipe(shareReplay(1));
    return this._messageSenders$;
  }

  public createMessageSender(request: CreateMessageSenderRequest): Observable<MessageSender> {
    return this._messagesApi.createMessageSender(request).pipe(tap(() => this._messageSenders$ = undefined));
  }

  public updateMessageSender(senderId: string, request: UpdateMessageSenderRequest): Observable<void> {
    return this._messagesApi.updateMessageSender(senderId, request).pipe(tap(() => this._messageSenders$ = undefined));
  }

  public deleteMessageSender(senderId: string): Observable<void> {
    return this._messagesApi.deleteMessageSender(senderId).pipe(tap(() => this._messageSenders$ = undefined));
  }

  public getMediaSetting(key: string): Observable<MediaSetting | undefined> {
    return this.listMediaSettings().pipe(map(settings => settings.find(s => s.key == key)));
  }

  public listMediaSettings(): Observable<MediaSetting[]> {
    this._mediaSettings$ ??= this._mediaApi.listMediaSettings().pipe(shareReplay(1));
    return this._mediaSettings$;
  }

  public updateMediaSettings(key: string, request: UpdateMediaSettingRequest): Observable<void> {
    return this._mediaApi.updateMediaSetting(key, request).pipe(tap(() => this._mediaSettings$ = undefined));
  }
}
