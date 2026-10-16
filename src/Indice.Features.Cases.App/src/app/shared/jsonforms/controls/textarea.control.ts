import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TailwindControl } from './tailwind-control';

/** ajsf `textarea` widget. */
@Component({
    selector: 'app-jf-textarea',
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <div class="mt-1">
          <textarea
            class="block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
            [class]="fieldClass"
            [rows]="options.rows || 4"
            [attr.maxlength]="options.maxLength"
            [attr.minlength]="options.minLength"
            [attr.placeholder]="options.placeholder"
            [attr.required]="options.required"
            [id]="id"
            [readonly]="readonly"
            [value]="data ?? ''"
            (input)="setValue($any($event.target).value)"></textarea>
        </div>
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class TextareaControlComponent extends TailwindControl {
    protected widget = 'textarea';
}
