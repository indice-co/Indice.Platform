import { Component, forwardRef, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import * as ClassicEditor from '@ckeditor/ckeditor5-build-classic';

@Component({
    selector: 'app-translate-input',
    templateUrl: './translate-input.component.html',
    providers: [{
        provide: NG_VALUE_ACCESSOR,
        useExisting: forwardRef(() => TranslateInputComponent),
        multi: true
    }]
})
export class TranslateInputComponent implements ControlValueAccessor, OnInit {
    private _translations: { [key: string]: string; } = {};
    private _selectedCulture: string;

    constructor() { }

    @Input() public id: string;
    @Input() public name: string;
    @Input() public required: boolean;
    @Input() public disabled: boolean;
    @Input() public placeholder: string;
    @Input() public cultures: string[] = [];
    @Input() public errorClass: string;
    @Input() public inputType = 'text'; // 'text' or 'textarea' or 'texteditor'
    @Input() public initialValues: { [key: string]: string; } = {}
    @Output() public translations: EventEmitter<{ [key: string]: string; }> = new EventEmitter();
    @Output() changedCulture: EventEmitter<string> = new EventEmitter();
    public currentValue: string;
    public editor = ClassicEditor;
    public config = {
        link: {
            decorators: {
                openInNewTab: {
                    mode: 'manual',
                    label: 'Open in a new tab',
                    attributes: {
                        target: '_blank',
                        rel: 'noopener noreferrer'
                    }
                }
            }
        }
    };

    @Input('selectedCulture')
    set selectedCulture(value: string) {
        this._selectedCulture = value;
        const currentValue = this._translations[this.selectedCulture] || this.initialValues[this.selectedCulture];
        this.currentValue = currentValue || '';
    }

    get selectedCulture(): string {
        return this._selectedCulture;
    }

    public onChange: (_: any) => void = (_: any) => { };

    public onTouched: () => void = () => { };

    public ngOnInit(): void {
        if (this.cultures.length > 0) {
            this.selectedCulture = this.cultures[0];
        }
    }

    public updateChanges(): void {
        this.onChange(this.currentValue);
        this._translations[this.selectedCulture] = this.currentValue;
        this.translations.emit(this._translations);
    }

    public writeValue(value: string): void {
        this.currentValue = value;
        this.updateChanges();
    }

    public registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    public setDisabledState?(isDisabled: boolean): void { }

    public cultureChanged(culture: string): void {
        this.selectedCulture = culture;
        this.changedCulture.emit(this.selectedCulture)
    }
}