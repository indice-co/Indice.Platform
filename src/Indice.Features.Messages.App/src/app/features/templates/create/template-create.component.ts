import { AfterViewChecked, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';

import { HeaderMetaItem, Icons } from '@indice/ng-components';
import { MessageChannelKind } from 'src/app/core/services/messages-api.service';
import { CampaignContentComponent } from '../../campaigns/create/steps/content/campaign-content.component';

@Component({
    selector: 'app-template-create',
    templateUrl: './template-create.component.html'
})
export class TemplateCreateComponent implements OnInit, AfterViewChecked {
    @ViewChild('contentStep', { static: true }) private _contentStep!: CampaignContentComponent;

    constructor(
        private _changeDetector: ChangeDetectorRef
    ) { }

    public metaItems: HeaderMetaItem[] | null = [];
    public basicInfoData: any = {};
    public saveInProgress = false;

    public ngOnInit(): void {
        this.metaItems = [
            { key: 'info', icon: Icons.Details, text: 'Ακολουθήστε τα παρακάτω βήματα για να δημιουργήσετε ένα νέο πρότυπο.' }
        ];
        this._contentStep.init([
            { channel: MessageChannelKind.Inbox, checked: true },
            { channel: MessageChannelKind.Email, checked: true }
        ]);
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }
}
