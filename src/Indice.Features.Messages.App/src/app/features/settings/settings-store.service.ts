import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CreateMessageSenderRequest, MessageSender, MessageSenderResultSet, MessagesApiClient, UpdateMessageSenderRequest } from 'src/app/core/services/messages-api.service';

@Injectable({
    providedIn: 'root'
})
export class SettingsStore {
    private _messageSenders: AsyncSubject<MessageSenderResultSet> | undefined;

    constructor(
        private _api: MessagesApiClient
    ) { }

    public getMessageSenders() {
        if (!this._messageSenders) {
            this._messageSenders = new AsyncSubject<MessageSenderResultSet>();
            this._api.getMessageSenders()
            .subscribe((result) => {
                this._messageSenders?.next(result);
                this._messageSenders?.complete();
            });
        }
        return this._messageSenders;
    }

    public createMessageSender(request: CreateMessageSenderRequest): Observable<MessageSender> {
        return this._api.createMessageSender(request)
            .pipe(
                map(response => {
                    this._messageSenders = undefined;
                    return response;
                })
            );
    }

    public updateMessageSender(senderId: string, request: UpdateMessageSenderRequest): Observable<void> {
        return this._api.updateMessageSender(senderId, request)
            .pipe(
                map(_ => this._messageSenders = undefined)
            );
    }
}
