import { Component, input, model } from '@angular/core';

export interface RadioOption {
  text: string;
  value: any;
  description?: string;
}

/** A vertical list of selectable options with optional descriptions. Two-way bind with `[(value)]`. */
@Component({
  selector: 'app-radio-buttons-list',
  host: { class: 'block' },
  template: `
    <fieldset class="space-y-2">
      <legend class="mb-2 text-sm font-medium">{{ title() }}</legend>
      @for (option of options(); track option.value) {
        <label class="flex cursor-pointer items-start gap-3 rounded-field border p-3 transition-colors"
          [class]="value() === option.value ? 'border-primary bg-primary/5' : 'border-base-300 hover:bg-base-200'">
          <input type="radio" class="radio radio-primary radio-sm mt-0.5" [name]="name()" [value]="option.value"
            [checked]="value() === option.value" (change)="value.set(option.value)" />
          <span class="min-w-0">
            <span class="block text-sm font-medium">{{ option.text }}</span>
            @if (option.description) {
              <span class="block text-xs text-base-content/65">{{ option.description }}</span>
            }
          </span>
        </label>
      }
    </fieldset>
  `
})
export class RadioButtonsListComponent {
  public readonly title = input<string>();
  public readonly name = input('radio-options');
  public readonly options = input<RadioOption[]>([]);
  public readonly value = model<any>();
}
