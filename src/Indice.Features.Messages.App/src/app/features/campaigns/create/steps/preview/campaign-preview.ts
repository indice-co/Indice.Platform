import { Hyperlink, Period } from 'src/app/core/services/messages-api.service';

export class CampaignPreview {
    constructor(
        public title?: string, 
        public type?: string, 
        public period?: Period, 
        public actionLink?: Hyperlink, 
        public template?: string, 
        public distributionList?: string
    ) { }
}
