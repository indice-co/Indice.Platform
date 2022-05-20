import { Injectable } from '@angular/core';
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

import { UtilitiesService } from 'src/app/shared/utilities.service';

@Injectable({
    providedIn: 'root'
})
export class ValidationService {
    constructor(private _utilities: UtilitiesService) { }

    public invalidJsonValidator(): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => 
            this._utilities.isValidJson(control.value) ? null : { invalidJson: { value: control.value } };
    }
}
