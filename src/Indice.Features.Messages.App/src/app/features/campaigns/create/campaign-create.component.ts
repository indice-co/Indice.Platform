import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';

import { MenuOption, Modal, ToasterService, ToastType } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import * as app from 'src/app/core/models/settings';
import { Campaign, CreateCampaignRequest, Period, ValidationProblemDetails, MessagesApiClient, MessageChannelKind, MessageTypeResultSet, MessageContent, Hyperlink } from 'src/app/core/services/messages-api.service';
import { UtilitiesService } from 'src/app/shared/utilities.service';

@Component({
    selector: 'app-campaign-create',
    templateUrl: './campaign-create.component.html'
})
export class CampaignCreateComponent implements OnInit, AfterViewInit {
    @ViewChild('campaignForm', { static: false }) private _campaignForm!: NgForm;
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

    constructor(
        private _api: MessagesApiClient,
        private _router: Router,
        private _utilities: UtilitiesService,
        private _changeDetector: ChangeDetectorRef,
        @Inject(ToasterService) private _toaster: ToasterService,
        @Inject(DOCUMENT) private _document: Document
    ) { }

    public now: Date = new Date();
    public model = new CreateCampaignRequest({
        activePeriod: new Period({ from: this.now }),
        published: false,
        isGlobal: true,
        messageChannelKind: [MessageChannelKind.Inbox],
        title: '',
        content: { 'inbox': new MessageContent() },
        actionLink: new Hyperlink({ href: undefined, text: undefined }),
        templateId: undefined
    });
    public messageTypes: MenuOption[] = [];
    public campaignTypesModalRef: Modal | undefined;
    public isDevelopment = !app.settings.production;
    public MessageChannelKind = MessageChannelKind;
    public customDataValid = true;
    public showCustomDataValidation = false;
    public submitInProgress = false;
    public targetOptions: MenuOption[] = [new MenuOption('Όλους τους χρήστες', true), new MenuOption('Ομάδα χρηστών', false)];

    public ngOnInit(): void {
        // Did not find any better way to do this.
        setTimeout(() => {
            const sidePane = this._document.getElementsByClassName('side-pane-box-size')[0] as HTMLElement;
            sidePane.style.maxWidth = '84rem';
        }, 0);
        this.loadMessageTypes();
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public canSubmit(): boolean {
        return this._campaignForm?.valid === true && this.hasMessageChannelKind();
    }

    public onSubmit(): void {
        if (!this.canSubmit()) {
            return;
        }
        this.submitInProgress = true;
        this._api
            .createCampaign(this.model)
            .subscribe({
                next: (campaign: Campaign) => {
                    this.submitInProgress = false;
                    // This is to force reload campaigns page when a new campaign is successfully saved. 
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η καμπάνια με τίτλο '${campaign.title}' δημιουργήθηκε με επιτυχία.`);
                },
                error: (problemDetails: ValidationProblemDetails) => {
                    this._toaster.show(ToastType.Error, 'Αποτυχής αποθήκευση', `${this._utilities.getValidationProblemDetails(problemDetails)}`, 6000);
                }
            });
    }

    public toDate(event: any): Date | undefined {
        var value = event.target.value
        if (value) {
            return new Date(value);
        }
        return undefined;
    }

    public setIsGlobal(isGlobal: boolean): void {
        this.model.isGlobal = isGlobal;
        if (isGlobal) {
            delete this.model.recipientIds;
        }
    }

    public toRecipientIdsArray(recipientIds: string | undefined): string[] {
        return recipientIds ? [...new Set(recipientIds.split('\n').filter(x => x !== ''))] : [];
    }

    public toRecipientIdsString(recipientIds: string[] | undefined): string {
        return recipientIds ? recipientIds.join('\n') : '';
    }

    public toggleMessageChannelKind(messageChannelKind: MessageChannelKind): void {
        const index = this.model.messageChannelKind!.findIndex(channel => channel === messageChannelKind);
        if (index > -1) {
            this.model.messageChannelKind!.splice(index, 1);
        } else {
            this.model.messageChannelKind!.push(messageChannelKind);
        }
    }

    public hasMessageChannelKind(): boolean {
        return this.model.messageChannelKind!.length > 0;
    }

    public containsMessageChannelKind(messageChannelKind: MessageChannelKind): boolean {
        return this.model.messageChannelKind!.indexOf(messageChannelKind) > -1;
    }

    public setCampaignCustomData(metadataJson: string): void {
        if (!metadataJson || metadataJson === '') {
            if ('data' in this.model) {
                delete this.model.data;
            }
            return;
        }
        try {
            const data = JSON.parse(metadataJson);
            this.customDataValid = true;
            this.model.data = data;
        } catch (error) {
            this.customDataValid = false;
        }
    }

    public onCustomDataFocusOut(): void {
        this.showCustomDataValidation = true;
    }

    private loadMessageTypes(): void {
        this.messageTypes = [];
        this._api
            .getMessageTypes()
            .pipe(map((messageTypes: MessageTypeResultSet) => {
                if (messageTypes.items) {
                    this.messageTypes = messageTypes.items.map(type => new MenuOption(type.name || '', type.id));
                    this.messageTypes.unshift(new MenuOption('Παρακαλώ επιλέξτε...', null));
                }
            }))
            .subscribe();
    }
}
