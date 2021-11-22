import { Component } from '@angular/core';

import { MenuOption, Modal } from '@indice/ng-components';

@Component({
    selector: 'app-campaign-types-modal',
    templateUrl: './campaign-types.component.html'
})
export class CampaignTypesModalComponent {
    constructor(public modalRef: Modal) { }

    public campaignTypes: MenuOption[] = [];
}
