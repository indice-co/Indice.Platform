import { Component, HostListener, Inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { CampaignContentComponent } from 'src/app/features/campaigns/create/steps/content/campaign-content.component';
import { MessageContent, Template } from 'src/app/core/services/messages-api.service';
import { TemplateEditStore } from '../template-edit-store.service';

@Component({
    selector: 'app-template-content-edit',
    templateUrl: './template-edit-content.component.html'
})
export class TemplateContentEditComponent implements OnInit {
    @ViewChild('contentStep', { static: false }) public _contentComponent: CampaignContentComponent | undefined;
    private _templateId: string = '';

    constructor(
        private _templateStore: TemplateEditStore,
        private _activatedRoute: ActivatedRoute,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public basicInfoData: any = {};
    public template = new Template();
    public content: { [key: string]: MessageContent; } | undefined = undefined;
    public updateInProgress = false;

    public ngOnInit(): void {
        this._templateId = this._activatedRoute.parent?.snapshot.params['templateId'];
        if (this._templateId) {
            this._templateStore.getTemplate(this._templateId!).subscribe((template: Template) => {
                this.template = template;
                this.content = template.content;
                this.basicInfoData = template.data ?? { };
            });
        }
    }

    @HostListener('document:keydown.control.s', ['$event']) onKeydownHandler(event: KeyboardEvent) {
      event.preventDefault();
      this.updateContent();
    }

    public updateContent(): void {
        this.updateInProgress = true;
        const formContents = this._contentComponent?.form.controls.content.value;
        const dataContents = this._contentComponent?.form.controls.data.value ?? "{}";
        let content: { [key: string]: MessageContent; } = {};
        for (const item of formContents) {
            content[item.channel] = new MessageContent({
                title: item.subject,
                sender: item.sender,
                body: item.body
            })
        }
        this.template.content = content;
        this.template.data = JSON.parse(dataContents);
        this._templateStore
            .updateTemplate(this._templateId, this.template)
            .subscribe(_ => {
                this.updateInProgress = false;
                this._toaster.show(ToastType.Success, 'Επιτυχής ενημέρωση', `Το περιεχόμενο του προτύπου με όνομα '${this.template.name}' ενημερώθηκε με επιτυχία.`);
            });
    }
}
