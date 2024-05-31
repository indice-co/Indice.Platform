import { DatePipe } from '@angular/common';
import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Params, Router } from '@angular/router';

import { ComboboxComponent, MenuOption, ToasterService, ToastType } from '@indice/ng-components';
import { Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { CampaignDetails, DistributionList, DistributionListResultSet, Hyperlink, MessagesApiClient, MessageSender, MessageSenderResultSet, MessageType, MessageTypeResultSet, Period } from 'src/app/core/services/messages-api.service';
import { CampaignEditStore } from '../../campaign-edit-store.service';
import { SettingsStore } from 'src/app/features/settings/settings-store.service';
import { TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'app-campaign-details-edit-rightpane',
    templateUrl: './campaign-edit-details-rightpane.component.html'
})
export class CampaignDetailsEditRightpaneComponent implements OnInit, AfterViewInit, OnDestroy {
    @ViewChild('editTitleTemplate', { static: true }) public editTitleTemplate!: TemplateRef<any>;
    @ViewChild('editTypeTemplate', { static: true }) public editTypeTemplate!: TemplateRef<any>;
    @ViewChild('editSenderTemplate', { static: true }) public editSenderTemplate!: TemplateRef<any>;
    @ViewChild('editActivePeriodTemplate', { static: true }) public editActivePeriodTemplate!: TemplateRef<any>;
    @ViewChild('editCtaTemplate', { static: true }) public editCtaTemplate!: TemplateRef<any>;
    @ViewChild('editListTemplate', { static: true }) public editListTemplate!: TemplateRef<any>;
    @ViewChild('editCampaignForm', { static: true }) public editCampaignForm!: NgForm;
    @ViewChild('distributionListCombobox', { static: false }) private _distributionListCombobox!: ComboboxComponent;
    private _updateCampaignSubscription: Subscription | undefined;
    private _campaignId = '';
    private _distributionLists: DistributionList[] = [];

    constructor(
        private _campaignStore: CampaignEditStore,
        private _router: Router,
        private _translate: TranslateService,
        private _activatedRoute: ActivatedRoute,
        private _changeDetector: ChangeDetectorRef,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _api: MessagesApiClient,
        private _datePipe: DatePipe,
        private _settingsStore: SettingsStore
    ) { }

    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    public submitInProgress = false;
    public templateOutlet!: TemplateRef<any>;
    public model = new CampaignDetails();
    public messageTypes: MenuOption[] = [new MenuOption(this._translate.instant('general.please-choose'), null)];
    public messageSenders: MenuOption[] = [new MenuOption(this._translate.instant('general.please-choose'), null)];
    public selectedTypeId: MenuOption | null = null;
    public selectedSenderId: MenuOption | null = null;
    public now: Date = new Date();
    public activePeriodFrom: string | null = null;
    public activePeriodTo: string | null = null;
    public distributionListsLoading = false;
    public distributionLists: string[] = [];

    public ngOnInit(): void {
        this._campaignId = this._router.url.split('/')[2];
        this._activatedRoute.queryParams.subscribe((queryParams: Params) => {
            this._selectTemplate(queryParams.action || 'editTitle');
        });
    }

    public typeSelectionChanged(selectedOption: MenuOption): void {
        if (selectedOption.value) {
            this.selectedTypeId = selectedOption;
            if (!this.model.type) {
                this.model.type = new MessageType();
            }
            this.model.type.id = selectedOption.value;
        } else {
            this.selectedTypeId = null;
            this.model.type = undefined;
        }
    }

    public senderSelectionChanged(selectedOption: MenuOption): void {
        if (!this.model.content?.email) {
            return;
        }
        if (selectedOption.value) {
            this.selectedSenderId = selectedOption;
            if (!this.model.content?.email) {
                return;
            }
        } else {
            this.selectedSenderId = null;
        }
    }

    public onSubmit(): void {
        if (this.model.content?.email) {
            if (!this.selectedSenderId?.data && this.model.content.email.sender) {
                this.model.content.email.sender = undefined;
            }
            else if (this.selectedSenderId?.data && this.selectedSenderId.data.id != this.model.content.email.sender?.id) {
                this.model.content.email.sender = new MessageSender(this.selectedSenderId.data);
            }
        }
        this.submitInProgress = true;
        this._updateCampaignSubscription = this._campaignStore
            .updateCampaign(this._campaignId, this.model)
            .subscribe({
                next: () => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, this._translate.instant('campaigns-edit.success-save'), `'${this._translate.instant('campaigns-edit.success-save-message')}' '${this.model.title}'`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns', this._campaignId]));
                }
            });
    }

    public onCampaignStartInput(event: any): void {
        if (!this.model.activePeriod) {
            this.model.activePeriod = new Period();
        }
        this.model.activePeriod.from = new Date(event.target.value);
    }

    public onCampaignEndInput(event: any): void {
        if (!this.model.activePeriod) {
            this.model.activePeriod = new Period();
        }
        this.model.activePeriod.to = new Date(event.target.value);
    }

    public onDistributionListsSearch(searchTerm: string | undefined): void {
        this.distributionListsLoading = true;
        this._api
            .getDistributionLists(1, 10, 'name', searchTerm)
            .subscribe((distributionLists: DistributionListResultSet) => {
                this._distributionLists = distributionLists.items || [];
                this.distributionLists = this._distributionLists.map(x => x.name!);
                this.distributionListsLoading = false;
            });
    }

    public onDistributionListSelected(name: string): void {
        if (!this.model.distributionList) {
            this.model.distributionList = new DistributionList();
        }
        this.model.distributionList.id = this._distributionLists.find(x => x.name === name)?.id;
        this.model.isGlobal = false;
    }

    public ngAfterViewInit(): void {
        this._getCampaign();
        this._changeDetector.detectChanges();
    }

    private _getCampaign(): void {
        this._campaignStore.getCampaign(this._campaignId).subscribe((campaign: CampaignDetails) => {
            this.model = new CampaignDetails(campaign);
            this.model.activePeriod = new Period({
                from: campaign.activePeriod?.from,
                to: campaign.activePeriod?.to
            });
            this.model.actionLink = new Hyperlink({
                text: campaign.actionLink?.text || '',
                href: campaign.actionLink?.href || ''
            });
            if (campaign.type?.id) {
                this.selectedTypeId = new MenuOption(campaign.type.name || '', campaign.type.id);
            }
            if (campaign.content?.email?.sender) {
                let sender = {
                    id: campaign.content.email.sender.id,
                    sender: campaign.content.email.sender.sender,
                    displayName: campaign.content.email.sender.displayName
                }
                this.selectedSenderId = new MenuOption(`${sender.displayName} <${sender.sender}>`, sender.id, undefined, sender);
            }
            this.activePeriodFrom = this._datePipe.transform(campaign.activePeriod?.from || this.now, 'yyyy-MM-ddTHH:mm');
            this.activePeriodTo = campaign.activePeriod?.to ? this._datePipe.transform(campaign.activePeriod.to, 'yyyy-MM-ddTHH:mm') : null;
            if (this._distributionListCombobox) {
                this._distributionListCombobox.value = campaign.distributionList?.name || '';
            }
        });
    }

    private _selectTemplate(action: string): void {
        switch (action) {
            case 'editTitle':
                this.templateOutlet = this.editTitleTemplate;
                break;
            case 'editType':
                this.templateOutlet = this.editTypeTemplate;
                this._loadMessageTypes();
                break;
            case 'editSender':
                this.templateOutlet = this.editSenderTemplate;
                this._loadMessageSenders();
            break;
            case 'editActivePeriod':
                this.templateOutlet = this.editActivePeriodTemplate;
                break;
            case 'editCta':
                this.templateOutlet = this.editCtaTemplate;
                break;
            case 'editList':
                this.templateOutlet = this.editListTemplate;
                break;
        }
    }

    private _loadMessageTypes(): void {
        this._api
            .getMessageTypes()
            .pipe(map((messageTypes: MessageTypeResultSet) => {
                if (messageTypes.items) {
                    this.messageTypes.push(...messageTypes.items.map(type => new MenuOption(type.name || '', type.id)));
                }
            }))
            .subscribe();
    }

    private _loadMessageSenders(): void {        
        this._settingsStore
            .getMessageSenders()
            .pipe(map((messageSenders: MessageSenderResultSet) => {
                if (messageSenders.items) {
                    this.messageSenders.push(...messageSenders.items.map(s => {
                        let sender = {
                            id: s.id,
                            sender: s.sender,
                            displayName: s.displayName
                        }
                        if (s.isDefault) {
                            this.selectedSenderId ??= new MenuOption(`${s.displayName} <${s.sender}>`, s.id, undefined, undefined);
                            return new MenuOption(`${s.displayName} <${s.sender}>`, s.id, undefined, undefined)
                        }
                        return new MenuOption(`${s.displayName} <${s.sender}>`, s.id, undefined, sender)
                    }));
                }
            }))
            .subscribe();
    }

    public ngOnDestroy(): void {
        this._updateCampaignSubscription?.unsubscribe();
    }
}
