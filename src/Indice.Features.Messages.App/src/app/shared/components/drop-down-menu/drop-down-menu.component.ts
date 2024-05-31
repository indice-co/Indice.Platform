import { Component, EventEmitter, forwardRef, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { MenuOption } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'lib-local-drop-down-menu',
    templateUrl: './drop-down-menu.component.html',
    providers: [{
        provide: NG_VALUE_ACCESSOR,
        useExisting: forwardRef(() => LocalDropDownMenuComponent),
        multi: true
    }]
})
export class LocalDropDownMenuComponent implements OnInit, ControlValueAccessor {
    private _onChange$: any | undefined = undefined;
    private _onTouched$: any | undefined = undefined;
    private _expanded = false;

    constructor(
        private _translate: TranslateService
    ) { }

    @Input() public options: MenuOption[] = [];
    @Input() public value: MenuOption | null = null;
    @Input() public multiple = false;
    @Input() public placeholder: string = this._translate.instant('general.please-choose');
    @Output() public change: EventEmitter<MenuOption> = new EventEmitter<MenuOption>();

    public get expanded(): boolean {
        return this._expanded;
    }

    public set expanded(value: boolean) {
        this._expanded = value;
    }

    public ngOnInit(): void { }

    public isSelected(option: MenuOption): boolean {
        return option != null && this.value != null && option.value === this.value;
    }

    public onClickOutside($event: any): void {
        this.expanded = false;
    }

    public selectOption(option: MenuOption): void {
        this.change.emit(option);
        this.expanded = false;
        if (this._onChange$) {
            this._onChange$(option?.value ? option : null);
        }
        if (this._onTouched$) {
            this._onTouched$();
        }
    }

    public writeValue(option: MenuOption | null): void {
        this.value = option?.value ? option : null;
    }

    public registerOnChange(fn: any): void {
        this._onChange$ = fn;
    }

    public registerOnTouched(fn: any): void {
        this._onTouched$ = fn;
    }
}
