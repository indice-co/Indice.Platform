import { UntypedFormGroup } from '@angular/forms';

import { CreateResourceRequest } from 'src/app/core/services/identity-api.service';

export interface ResourceWizardModel {
    form: UntypedFormGroup;
    apiResource: CreateResourceRequest;
    displayType: boolean;
    navigationOrigin: string;
}
