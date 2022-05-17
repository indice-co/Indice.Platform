import { Component, EventEmitter, forwardRef, Input, OnInit, Output } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
    selector: 'lib-toggle-button',
    templateUrl: './toggle-button.component.html',
    providers: [{
        provide: NG_VALUE_ACCESSOR,
        useExisting: forwardRef(() => ToggleButtonComponent),
        multi: true
    }]
})
export class ToggleButtonComponent implements OnInit, ControlValueAccessor {
    private _onChange$: any | undefined = undefined;
    private _onTouched$: any | undefined = undefined;

    constructor() { }

    @Input() value: boolean = false;
    @Output() valueChange: EventEmitter<boolean> = new EventEmitter(false);
    @Input('true-label') trueLabel = '';
    @Input('false-label') falseLabel = '';

    public writeValue(value: any): void {
        if (value) {
            this.value = value || false;
        }
    }

    public registerOnChange(fn: any): void {
        this._onChange$ = fn;
    }

    public registerOnTouched(fn: any): void {
        this._onTouched$ = fn;
    }

    public ngOnInit(): void { }

    public toggleValue(): void {
        this.value = !this.value;
        this.valueChange.emit(this.value);
        if (this._onChange$) {
            this._onChange$(this.value);
        }
        if (this._onTouched$) {
            this._onTouched$();
        }
    }
}
