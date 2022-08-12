import { Component, EventEmitter, forwardRef, Input, OnInit, Output } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
    selector: 'lib-toggle-button',
    templateUrl: './toggle-button.component.html',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => ToggleButtonComponent),
            multi: true
        }
    ]
})
export class ToggleButtonComponent implements OnInit, ControlValueAccessor {

    @Input() value: boolean = false;
    @Output() valueChange: EventEmitter<boolean> = new EventEmitter(false);
    @Input('true-label') trueLabel = '';
    @Input('false-label') falseLabel = '';

    private onChange$: any | undefined = undefined;
    private onTouched$: any | undefined = undefined;

    constructor() { }

    writeValue(obj: any): void {
        if (obj) {
            this.value = obj || false;
        }
    }

    registerOnChange(fn: any): void {
        this.onChange$ = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched$ = fn;
    }

    public ngOnInit(): void { }

    public toggleValue(): void {
        this.value = !this.value;
        this.valueChange.emit(this.value);
        if (this.onChange$) {
            this.onChange$(this.value);
        }
        if (this.onTouched$) {
            this.onTouched$();
        }
    }
}
