import { AfterViewChecked, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { HeaderMetaItem, Icons, ToasterService, ToastType } from '@indice/ng-components';
import { CreateTemplateRequest, MessageContent, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { CampaignContentComponent } from '../../campaigns/create/steps/content/campaign-content.component';

@Component({
    selector: 'app-template-create',
    templateUrl: './template-create.component.html'
})
export class TemplateCreateComponent implements OnInit, AfterViewChecked {
    @ViewChild('templateContent', { static: true }) private _templateContent: CampaignContentComponent | undefined;

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _router: Router
    ) { }

    public metaItems: HeaderMetaItem[] | null = [];
    public basicInfoData: any = {};
    public saveInProgress = false;
    public template = new CreateTemplateRequest();
    public content: { [key: string]: MessageContent; } | undefined = {
        'inbox': new MessageContent()
    };

    public ngOnInit(): void {
        this.metaItems = [
            { key: 'info', icon: Icons.Details, text: 'Ακολουθήστε τα παρακάτω βήματα για να δημιουργήσετε ένα νέο πρότυπο.' }
        ];
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }

    public saveTemplate(): void {
        this.saveInProgress = true;
        const name = 'test-name';
        const formContents = this._templateContent?.form.controls.content.value;
        const dataContents = this._templateContent?.form.controls.data.value;
        let content: { [key: string]: MessageContent; } = {};
        for (const item of formContents) {
            content[item.channel] = new MessageContent({
                title: item.subject,
                sender: item.sender,
                body: item.body
            })
        }
        this.template.content = content;
        this.template.data = JSON.parse(dataContents ?? "{}");
        this._api
            .createTemplate(new CreateTemplateRequest(this.template))
            .subscribe(_ => {
                this.saveInProgress = false;
                this._toaster.show(ToastType.Success, 'Επιτυχής ενημέρωση', `Το πρότυπο με όνομα '${name}' δημιουργήθηκε με επιτυχία.`);
                this._router.navigate(['templates']);
            });
    }
}
