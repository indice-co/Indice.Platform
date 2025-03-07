import { UntypedFormGroup } from "@angular/forms";

import { CreateUserRequest } from "src/app/core/services/identity-api.service";

export interface UserWizardModel {
  form: UntypedFormGroup;
  apiResource: CreateUserRequest;
  navigationOrigin: string;
}
