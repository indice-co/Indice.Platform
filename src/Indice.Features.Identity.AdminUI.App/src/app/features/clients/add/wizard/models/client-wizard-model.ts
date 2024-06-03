import { UntypedFormGroup } from '@angular/forms';

import { CreateClientRequest } from 'src/app/core/services/identity-api.service';

export interface ClientWizardModel {
    form: UntypedFormGroup;
    client: CreateClientRequest;
}
