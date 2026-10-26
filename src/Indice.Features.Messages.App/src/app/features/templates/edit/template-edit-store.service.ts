import { Injectable, inject, signal } from '@angular/core';
import { Observable, of, tap } from 'rxjs';
import { MessagesApiClient, Template, UpdateTemplateRequest, UpdateTemplateUserPreferencesRequest } from 'src/app/core/services/messages-api.service';

/**
 * Holds the template being edited, shared by the edit page, its tabs and the edit side pane.
 * Every successful update reloads it, so all of them show fresh data.
 */
@Injectable({ providedIn: 'root' })
export class TemplateEditStore {
  private readonly _api = inject(MessagesApiClient);

  public readonly template = signal<Template | undefined>(undefined);

  /** (Re)loads the template; clears the current one first when switching to another template. */
  public load(templateId: string): void {
    this.template.set(undefined);
    this._api.getTemplateById(templateId).subscribe(template => this.template.set(template));
  }

  /** The loaded template when it is the requested one, otherwise fetches it. */
  public getTemplate(templateId: string): Observable<Template> {
    const current = this.template();
    return current?.id === templateId
      ? of(current)
      : this._api.getTemplateById(templateId).pipe(tap(template => this.template.set(template)));
  }

  public updateTemplate(templateId: string, template: Template): Observable<void> {
    const body = new UpdateTemplateRequest({
      name: template.name,
      alias: template.alias,
      content: template.content,
      data: template.data
    });
    return this._api.updateTemplate(templateId, body).pipe(tap(() => this.load(templateId)));
  }

  public updateUserPreference(templateId: string, ignoreUserPreferences: boolean | undefined): Observable<void> {
    const body = new UpdateTemplateUserPreferencesRequest({ ignoreUserPreferences });
    return this._api.updateTemplateUserPreferences(templateId, body).pipe(tap(() => this.load(templateId)));
  }

  public updateTemplateMessageType(templateId: string, messageTypeId: string | undefined): Observable<void> {
    return this._api.updateTemplateMessageType(templateId, messageTypeId).pipe(tap(() => this.load(templateId)));
  }
}
