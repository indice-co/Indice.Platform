import { FormGroup } from '@angular/forms';

import { CreateResourceRequest } from 'src/app/core/services/identity-api.service';

export interface ApiResourceWizardModel {
    form: FormGroup;
    apiResource: CreateResourceRequest;
}
