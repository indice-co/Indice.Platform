import { AfterContentInit, Directive, ElementRef, Input } from '@angular/core';

@Directive({
    selector: 'form[formReadonly]'
})
export class FormReadonlyDirective implements AfterContentInit {
    private _readonly: boolean;
    private _form: HTMLFormElement;

    constructor(element: ElementRef) {
        this._form = element.nativeElement as HTMLFormElement;
    }

    @Input('formReadonly')
    public set setFormReadonly(value: boolean) {
        this._readonly = value;
    }

    public ngAfterContentInit(): void {
        if (!this._readonly) {
            return;
        }
        const inputElements = this._form.querySelectorAll('input:not([type="checkbox"])');
        const selectElements = this._form.getElementsByTagName('select');
        const textareaElements = this._form.getElementsByTagName('textarea');
        const checkboxElements = this._form.querySelectorAll('input[type="checkbox"]');
        const inputElementsArray = Array.prototype.slice.call(inputElements);
        const selectElementsArray = Array.prototype.slice.call(selectElements);
        const checkboxElementsArray = Array.prototype.slice.call(checkboxElements);
        const textareaElementsArray = Array.prototype.slice.call(textareaElements);
        for (const input of inputElementsArray.concat(textareaElementsArray)) {
            input.readOnly = true;
        }
        for (const select of selectElementsArray.concat(checkboxElementsArray)) {
            select.disabled = true;
        }
    }
}
