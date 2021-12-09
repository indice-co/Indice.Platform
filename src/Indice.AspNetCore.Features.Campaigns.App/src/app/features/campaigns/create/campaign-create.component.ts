import { Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';

import { MenuOption, Modal, ModalService, SideViewLayoutComponent, ToasterService, ToastType } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { Campaign, CampaignsApiService, CampaignTypeResultSet, CreateCampaignRequest, Period } from 'src/app/core/services/campaigns-api.services';
import { CampaignTypesModalComponent } from '../campaign-types-modal/campaign-types.component';

@Component({
    selector: 'app-campaigns',
    templateUrl: './campaign-create.component.html'
})
export class CampaignCreateComponent implements OnInit {
    @ViewChild('campaignForm', { static: false }) private campaignForm!: NgForm;
    @ViewChild('sideViewLayout', { static: false }) private sideViewLayout!: SideViewLayoutComponent;
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

    constructor(
        private api: CampaignsApiService,
        private modal: ModalService,
        @Inject(ToasterService) private toaster: ToasterService
    ) { }

    public now: Date = new Date();
    public model: CreateCampaignRequest = new CreateCampaignRequest({
        activePeriod: new Period({ from: this.now }),
        published: true, 
        isGlobal: true
    });
    public campaignTypes: MenuOption[] = [];
    public campaignTypesModalRef: Modal | undefined;
    public isDevelopment = !environment.production;
    public targetOptions: MenuOption[] = [
        new MenuOption('Όλους τους χρήστες', true),
        new MenuOption('Ομάδα χρηστών', false)
    ];

    public ngOnInit(): void {
        this.loadCampaignTypes();
    }

    public onSubmit(): void {
        if (!this.campaignForm.valid) {
            return;
        }
        this.api.createCampaign(this.model).subscribe((campaign: Campaign) => {
            this.sideViewLayout.emitClose();
            this.toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η καμπάνια με τίτλο '${campaign.title}' δημιουργήθηκε με επιτυχία.`);
        });
    }

    public openCampaignTypesModal(): void {
        this.campaignTypesModalRef = this.modal.show(CampaignTypesModalComponent, {
            backdrop: 'static',
            keyboard: false,
            animated: true,
            initialState: {
                campaignTypes: this.campaignTypes.filter(x => x.value != null)
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
            this.model.selectedUserCodes = [];
        }
    }

    public toUserCodesArray(userCodes: string | undefined): string[] {
        return userCodes ? [...new Set(userCodes.split('\n').filter(x => x !== ''))] : [];
    }

    public toUserCodesString(userCodes: string[] | undefined): string {
        return userCodes ? userCodes.join('\n') : '';
    }

    private loadCampaignTypes(): void {
        this.campaignTypes = [];
        this.api
            .getCampaignTypes()
            .pipe(map((campaignTypes: CampaignTypeResultSet) => {
                if (campaignTypes.items) {
                    this.campaignTypes = campaignTypes.items.map(type => new MenuOption(type.name || '', type.id));
                    this.campaignTypes.unshift(new MenuOption('Παρακαλώ επιλέξτε...', null));
                }
            }))
            .subscribe();
    }
}
