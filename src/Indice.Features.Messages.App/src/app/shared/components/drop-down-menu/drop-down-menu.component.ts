import { Component, EventEmitter, forwardRef, Inject, Input, OnInit, Output } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { APP_LANGUAGES, MenuOption } from '@indice/ng-components';
import { AppLanguagesService } from '../../services/app-languages.service';

@Component({
    selector: 'lib-local-drop-down-menu',
    templateUrl: './drop-down-menu.component.html',
    providers: [{
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => LocalDropDownMenuComponent),
            multi: true
        }],
    standalone: false
})
export class LocalDropDownMenuComponent implements ControlValueAccessor {
  private _onChange$: any | undefined = undefined;
  private _onTouched$: any | undefined = undefined;
  private _expanded = false;

  constructor(@Inject(APP_LANGUAGES) private _lang: AppLanguagesService) { }

  @Input() public options: MenuOption[] = [];
  @Input() public value: MenuOption | null = null;
  @Input() public multiple = false;
  @Input() public placeholder: string = 'Shared.SelectPlaceholder'; // fallback initial value (Greek)
  @Output() public change: EventEmitter<MenuOption> = new EventEmitter<MenuOption>();

  public get expanded(): boolean {
    return this._expanded;
  }

  public set expanded(value: boolean) {
    this._expanded = value;
  }

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
