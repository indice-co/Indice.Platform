import { Component } from '@angular/core';

import { MenuOption, Modal } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { CampaignsApiService, CampaignType, UpsertCampaignTypeRequest } from 'src/app/core/services/campaigns-api.services';

@Component({
    selector: 'app-campaign-types-modal',
    templateUrl: './campaign-types.component.html'
})
export class CampaignTypesModalComponent {
    private typeNameBeforeEdit = '';

    constructor(
        public modalRef: Modal,
        public api: CampaignsApiService
    ) { }

    public campaignTypes: MenuOption[] = [];
    public campaignTypesChanged: boolean = false; // Inform opener that changes have been made to campaign types, in order to reload.
    public isAddingNewType = false; // Boolean value that prevents from adding multiple new campaign types.

    public onAddNewType(): void {
        this.isAddingNewType = true;
        const type = new MenuOption('', '');
        (type as any).editMode = true;
        this.campaignTypes.push(type);
    }

    public onOpenEditMode(type: MenuOption): void {
        (type as any).editMode = true;
        this.typeNameBeforeEdit = type.text;
    }

    public onCloseEditMode(type: MenuOption): void {
        this.isAddingNewType = false;
        (type as any).editMode = false;
        const isNew = type.value === '';
        if (isNew) {
            const index = this.campaignTypes.length - 1;
            this.campaignTypes.splice(index, 1);
        } else {
            type.text = this.typeNameBeforeEdit;
        }
    }

    public onUpsertCampaignType(type: MenuOption): void {
        this.isAddingNewType = false;
        this.campaignTypesChanged = true;
        const isNew = type.value === '';
        const body = { name: type.text } as UpsertCampaignTypeRequest;
        if (isNew) {
            this.api.createCampaignType(body).subscribe((createdCampaign: CampaignType) => {
                (type as any).editMode = false;
                type.value = createdCampaign.id;
            });
        } else {
            this.api.updateCampaignType(type.value, body).subscribe(() => {
                (type as any).editMode = false;
            });
        }
    }

    public onDeleteCampaignType(type: MenuOption): void {
        const typeId = type.value;
        this.api
            .deleteCampaignType(typeId)
            .pipe(map(_ => {
                this.campaignTypesChanged = true;
                const foundCampaignType = this.campaignTypes.find(x => x.value === typeId);
                if (foundCampaignType) {
                    const index = this.campaignTypes.indexOf(foundCampaignType);
                    this.campaignTypes.splice(index, 1);
                }
            }))
            .subscribe();
    }
}
