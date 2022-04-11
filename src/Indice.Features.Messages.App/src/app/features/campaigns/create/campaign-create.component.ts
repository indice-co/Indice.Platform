import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';

import { MenuOption, Modal, ModalService, SideViewLayoutComponent, ToasterService, ToastType } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import * as app from 'src/app/core/models/settings';
import { Campaign, CreateCampaignRequest, Period, ValidationProblemDetails, MessagesApiClient, MessageChannelKind, MessageTypeResultSet, MessageContent, Hyperlink } from 'src/app/core/services/campaigns-api.services';
import { UtilitiesService } from 'src/app/shared/utilities.service';
import { CampaignTypesModalComponent } from '../campaign-types-modal/campaign-types.component';

@Component({
    selector: 'app-campaigns',
    templateUrl: './campaign-create.component.html'
})
export class CampaignCreateComponent implements OnInit, AfterViewInit {
    @ViewChild('campaignForm', { static: false }) private campaignForm!: NgForm;
    @ViewChild('sideViewLayout', { static: false }) private sideViewLayout!: SideViewLayoutComponent;
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

    constructor(
        private _api: MessagesApiClient,
        private _modal: ModalService,
        private _router: Router,
        public utilities: UtilitiesService,
        private _changeDetector: ChangeDetectorRef,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public now: Date = new Date();
    public model = new CreateCampaignRequest({
        activePeriod: new Period({
            from: this.now
        }),
        published: false,
        isGlobal: true,
        messageChannelKind: [MessageChannelKind.Inbox],
        title: '',
        content: {
            'inbox': new MessageContent()
        },
        actionLink: new Hyperlink({
            href: '',
            text: ''
        })
    });
    public messageTypes: MenuOption[] = [];
    public campaignTypesModalRef: Modal | undefined;
    public isDevelopment = !app.settings.production;
    public MessageChannelKind = MessageChannelKind;
    public customDataValid = true;
    public showCustomDataValidation = false;
    public submitInProgress = false;
    public targetOptions: MenuOption[] = [
        new MenuOption('Όλους τους χρήστες', true),
        new MenuOption('Ομάδα χρηστών', false)
    ];

    public ngOnInit(): void {
        this.loadCampaignTypes();
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public canSubmit(): boolean {
        return this.campaignForm?.valid === true && this.hasDeliveryChannel();
    }

    public onSubmit(): void {
        if (!this.canSubmit()) {
            return;
        }
        this.submitInProgress = true;
        this._api
            .createCampaign(this.model)
            .subscribe((campaign: Campaign) => {
                this.submitInProgress = false;
                // This is to force reload campaigns page when a new campaign is successfully saved. 
                this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
                this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η καμπάνια με τίτλο '${campaign.title}' δημιουργήθηκε με επιτυχία.`);
            }, (problemDetails: ValidationProblemDetails) => {
                // This is to force reload campaigns page when a new campaign is successfully saved.
                this._toaster.show(ToastType.Error, 'Αποτυχής αποθήκευση', `${this.utilities.getValidationProblemDetails(problemDetails)}`, 6000);
            });
    }

    public openCampaignTypesModal(): void {
        this.campaignTypesModalRef = this._modal.show(CampaignTypesModalComponent, {
            backdrop: 'static',
            keyboard: false,
            animated: true,
            initialState: {
                campaignTypes: this.messageTypes.filter(x => x.value != null)
            }
        });
        this.campaignTypesModalRef.onHidden?.subscribe((response: any) => {
            if (response.result.campaignTypesChanged) {
                this.loadCampaignTypes();
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

    public toUserCodesArray(userCodes: string | undefined): string[] {
        return userCodes ? [...new Set(userCodes.split('\n').filter(x => x !== ''))] : [];
    }

    public toUserCodesString(userCodes: string[] | undefined): string {
        return userCodes ? userCodes.join('\n') : '';
    }

    public toggleDeliveryChannel(deliveryType: MessageChannelKind): void {
        if (deliveryType !== MessageChannelKind.Inbox) {
            this.model.messageChannelKind = this.model.messageChannelKind!.filter(x => x === MessageChannelKind.Inbox || x === deliveryType);
        }
        const index = this.model.messageChannelKind!.findIndex(channel => channel === deliveryType);
        if (index > -1) {
            this.model.messageChannelKind!.splice(index, 1);
        } else {
            this.model.messageChannelKind!.push(deliveryType);
        }
    }

    public hasDeliveryChannel(): boolean {
        return this.model.messageChannelKind!.length > 0;
    }

    public containsDeliveryChannel(deliveryType: MessageChannelKind): boolean {
        return this.model.messageChannelKind!.indexOf(deliveryType) > -1;
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

    private loadCampaignTypes(): void {
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
