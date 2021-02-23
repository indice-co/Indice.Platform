import { Component, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { ValueType } from 'src/app/core/services/identity-api.service';

@Component({
    selector: 'app-dynamic-input',
    templateUrl: './dynamic-input.component.html',
    providers: [{
        provide: NG_VALUE_ACCESSOR,
        useExisting: forwardRef(() => DynamicInputComponent),
        multi: true
    }]
})
export class DynamicInputComponent implements ControlValueAccessor {
    constructor() { }

    @Input() public id: string;
    @Input() public name: string;
    @Input() public modelType: ValueType;
    @Input() public placeholder: string;
    /**
     * Holds the current value of the input.
     */
    public value: string;
    /**
     * Invoked when the model has been changed.
     */
    public onChange: (_: any) => void = (_: any) => { };
    /**
     * Invoked when the model has been touched.
     */
    public onTouched: () => void = () => { };

    /**
     * Method that is invoked on an update of a model.
     */
    public updateChanges(): void {
        this.onChange(this.value);
    }

    // #region Overrides
    public writeValue(value: string): void {
        this.value = value;
        this.updateChanges();
    }

    public registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    public setDisabledState?(isDisabled: boolean): void { }
    // #endregion
}
