import { FormGroup } from '@angular/forms';

import { CreateClientRequest } from 'src/app/core/services/identity-api.service';

export interface WizardFormModel {
    form: FormGroup;
    client: CreateClientRequest;
}
