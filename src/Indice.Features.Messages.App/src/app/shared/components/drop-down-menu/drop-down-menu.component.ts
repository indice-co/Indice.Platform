import { Component, EventEmitter, forwardRef, Input, OnInit, Output } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Subject, combineLatest } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { MenuOption } from '@indice/ng-components';
import { AppLanguagesService } from '../../services/app-languages.service';

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
  private readonly destroy$ = new Subject<void>();

  constructor(private appLanguages: AppLanguagesService) { }

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

  public ngOnInit(): void {
    // Dynamically translate placeholder, fallback already set.
    combineLatest([
      this.appLanguages.translateKey(this.placeholder)
    ])
      .pipe(takeUntil(this.destroy$))
      .subscribe(([translatedPlaceholder]) => {
        this.placeholder = translatedPlaceholder || this.placeholder;
      });
  }

  public ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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
