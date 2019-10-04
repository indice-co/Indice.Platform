import { ClientType as Type } from 'src/app/core/services/identity-api.service';

export class ClientType {
    constructor(
        public key: Type,
        public name: string,
        public flowDescription: string,
        public descriptionHtml: string,
        public icon: string
    ) { }
}
