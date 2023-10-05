import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { MediaApiClient, MediaSetting, UpdateMediaSettingRequest } from 'src/app/core/services/media-api.service';
import { CreateMessageSenderRequest, MessageSender, MessageSenderResultSet, MessagesApiClient, UpdateMessageSenderRequest } from 'src/app/core/services/messages-api.service';

@Injectable({
    providedIn: 'root'
})
export class SettingsStore {
    private _messageSenders: AsyncSubject<MessageSenderResultSet> | undefined;
    private _mediaSettings: AsyncSubject<MediaSetting[]> | undefined;

    constructor(
        private _messagesApi: MessagesApiClient,
        private _mediaApi: MediaApiClient
    ) { }

    public getMessageSenders() {
        if (!this._messageSenders) {
            this._messageSenders = new AsyncSubject<MessageSenderResultSet>();
            this._messagesApi.getMessageSenders()
            .subscribe((result) => {
                this._messageSenders?.next(result);
                this._messageSenders?.complete();
            });
        }
        return this._messageSenders;
    }

    public createMessageSender(request: CreateMessageSenderRequest): Observable<MessageSender> {
        return this._messagesApi.createMessageSender(request)
            .pipe(
                map(response => {
                    this._messageSenders = undefined;
                    return response;
                })
            );
    }

    public updateMessageSender(senderId: string, request: UpdateMessageSenderRequest): Observable<void> {
        return this._messagesApi.updateMessageSender(senderId, request)
            .pipe(
                map(_ => this._messageSenders = undefined)
            );
    }

    public getMediaSetting(key: string) {
        return this.listMediaSettings()
            .pipe(map((settings) => settings.find(s => s.key == key)));
    }

    public listMediaSettings(): Observable<MediaSetting[]> {
        if (!this._mediaSettings) {
            this._mediaSettings = new AsyncSubject<MediaSetting[]>();
            this._mediaApi.listMediaSettings()
            .subscribe((result) => {
                this._mediaSettings?.next(result);
                this._mediaSettings?.complete();
            });
        }
        return this._mediaSettings;
    }

    public updateMediaSettings(key: string, request: UpdateMediaSettingRequest): Observable<void> {
        return this._mediaApi.updateMediaSetting(key, request)
            .pipe(
                map(_ => this._mediaSettings = undefined)
            );
    }
}
