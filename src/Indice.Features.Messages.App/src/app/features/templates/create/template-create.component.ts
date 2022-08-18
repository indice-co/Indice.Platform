import { AfterViewChecked, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';

import { HeaderMetaItem, Icons } from '@indice/ng-components';
import { MessageChannelKind, MessageContent } from 'src/app/core/services/messages-api.service';
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
}
