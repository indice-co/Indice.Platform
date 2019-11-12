import { FormGroup } from '@angular/forms';

import { CreateResourceRequest } from 'src/app/core/services/identity-api.service';

export interface ResourceWizardModel {
    form: FormGroup;
    apiResource: CreateResourceRequest;
    displayType: boolean;
    navigationOrigin: string;
}
