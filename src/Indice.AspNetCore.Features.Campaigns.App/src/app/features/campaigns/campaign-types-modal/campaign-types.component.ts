import { Component } from '@angular/core';

import { MenuOption, Modal } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { CampaignsApiService } from 'src/app/core/services/campaigns-api.services';

@Component({
    selector: 'app-campaign-types-modal',
    templateUrl: './campaign-types.component.html'
})
export class CampaignTypesModalComponent {
    constructor(
        public modalRef: Modal,
        public api: CampaignsApiService
    ) { }

    public campaignTypes: MenuOption[] = [];
    public campaignTypesChanged: boolean = false;

    public deleteCampaignType(campaignTypeId: string): void {
        this.api
            .deleteCampaignType(campaignTypeId)
            .pipe(map(_ => {
                this.campaignTypesChanged = true;
                const campaignType = this.campaignTypes.find(x => x.value === campaignTypeId);
                if (campaignType) {
                    const index = this.campaignTypes.indexOf(campaignType);
                    this.campaignTypes.splice(index, 1);
                }
            }))
            .subscribe();
    }
}
