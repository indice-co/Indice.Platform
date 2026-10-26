import { Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { ClaimValueType } from 'src/app/core/services/identity-api.service';
import { claimDateToInput, inputDateToClaim } from './user-utils';

/** Form control for a claim value that renders the input matching the claim's value type. Values are always strings. */
@Component({
  selector: 'app-claim-value-input',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => ClaimValueInputComponent), multi: true }],
  host: { class: 'block' },
  template: `
    @switch (valueType()) {
      @case ('Boolean') {
        <select class="select w-full" [class.select-error]="invalid()" [disabled]="disabled()" [attr.id]="inputId()"
          (change)="update($any($event.target).value)" (blur)="touched()">
          <option value="" [selected]="!value()">Select a value…</option>
          <option value="true" [selected]="value() === 'true'">Yes</option>
          <option value="false" [selected]="value() === 'false'">No</option>
        </select>
      }
      @case ('Int') {
        <input type="number" step="1" class="input w-full tabular" [class.input-error]="invalid()" [disabled]="disabled()"
          [attr.id]="inputId()" [value]="value()" [placeholder]="placeholder()" (input)="update($any($event.target).value)" (blur)="touched()" />
      }
      @case ('DateTime') {
        <input type="date" class="input w-full" [class.input-error]="invalid()" [disabled]="disabled()" [attr.id]="inputId()"
          [value]="dateValue()" (change)="update(toClaimDate($any($event.target).value))" (blur)="touched()" />
      }
      @default {
        <input type="text" class="input w-full" [class.input-error]="invalid()" [disabled]="disabled()" [attr.id]="inputId()"
          [value]="value()" [placeholder]="placeholder()" (input)="update($any($event.target).value)" (blur)="touched()" />
      }
    }
  `
})
export class ClaimValueInputComponent implements ControlValueAccessor {
  public readonly valueType = input<ClaimValueType | undefined>(ClaimValueType.String);
  public readonly placeholder = input('');
  public readonly invalid = input(false);
  public readonly inputId = input<string>();

  protected readonly value = signal('');
  protected readonly disabled = signal(false);
  private _onChange: (value: string) => void = () => void 0;
  private _onTouched: () => void = () => void 0;

  protected dateValue(): string {
    return claimDateToInput(this.value());
  }

  protected toClaimDate(value: string): string {
    return inputDateToClaim(value);
  }

  protected update(value: string): void {
    this.value.set(value ?? '');
    this._onChange(this.value());
  }

  protected touched(): void {
    this._onTouched();
  }

  public writeValue(value: string | null): void {
    this.value.set(value ?? '');
  }

  public registerOnChange(fn: (value: string) => void): void {
    this._onChange = fn;
  }

  public registerOnTouched(fn: () => void): void {
    this._onTouched = fn;
  }

  public setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }
}
