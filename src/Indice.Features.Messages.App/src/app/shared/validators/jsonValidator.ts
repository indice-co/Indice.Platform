import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function invalidJsonValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
        const value = control.value;
        if (!value || value === '') {
            return null;
        }
        try {
            JSON.parse(value);
            return null;
        } catch (error) {
            return { invalidJson: { value: control.value } };
        }
    };
}
