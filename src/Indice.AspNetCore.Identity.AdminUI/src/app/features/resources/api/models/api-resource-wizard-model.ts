import { FormGroup } from '@angular/forms';

import { CreateApiResourceRequest } from 'src/app/core/services/identity-api.service';

export interface ApiResourceWizardModel {
    form: FormGroup;
    apiResource: CreateApiResourceRequest;
}
