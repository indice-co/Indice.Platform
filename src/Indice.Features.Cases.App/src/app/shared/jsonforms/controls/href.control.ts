import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TailwindControl } from './tailwind-control';

/** ajsf `href` widget: a text input, shown as a link when read-only. */
@Component({
    selector: 'app-jf-href',
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (!readonly) {
          <input
            type="text"
            [class]="fieldClass"
            [attr.maxlength]="options.maxLength"
            [attr.minlength]="options.minLength"
            [attr.pattern]="options.pattern"
            [attr.placeholder]="options.placeholder"
            [attr.required]="options.required"
            [id]="id"
            [value]="data ?? ''"
            (input)="setValue($any($event.target).value)">
        } @else {
          <div class="mb-2 read-only:bg-gray-100 border rounded px-3 py-2">
            <a [href]="data" class="text-blue-600" target="_blank">{{ data }}</a>
          </div>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class HrefControlComponent extends TailwindControl {
    protected widget = 'href';
}
