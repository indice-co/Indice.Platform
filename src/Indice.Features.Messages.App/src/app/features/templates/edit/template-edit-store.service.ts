import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { MessagesApiClient, Template, UpdateTemplateRequest } from 'src/app/core/services/messages-api.service';

@Injectable({
    providedIn: 'root'
})
export class TemplateEditStore {
    private _template: AsyncSubject<Template> | undefined;
    private _idChanged = false;
    private _currentId = '';

    constructor(
        private _api: MessagesApiClient
    ) { }

    public getTemplate(templateId: string): Observable<Template> {
        this._idChanged = this._currentId !== templateId;
        this._currentId = templateId;
        if (!this._template || this._idChanged) {
            this._template = new AsyncSubject<Template>();
            this._api
                .getTemplateById(templateId)
                .subscribe((template: Template) => {
                    this._template?.next(template);
                    this._template?.complete();
                });
        }
        return this._template;
    }

    public updateTemplate(templateId: string, template: Template): Observable<void> {
        const body = new UpdateTemplateRequest({
            name: template.name,
            content: template.content
        });
        return this._api
            .updateTemplate(templateId, body)
            .pipe(
                map(_ => this._template = undefined)
            );
    }
}
