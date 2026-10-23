import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

/** ajsf `textarea` widget. */
@Component({
    selector: 'app-jf-textarea',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (showEditor) {
          <textarea
            [class]="fieldClass"
            [rows]="options.rows || 4"
            [attr.maxlength]="options.maxLength"
            [attr.minlength]="options.minLength"
            [attr.placeholder]="options.placeholder"
            [attr.required]="options.required"
            [id]="id"
            [value]="data ?? ''"
            (input)="setValue($any($event.target).value)"></textarea>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class TextareaControlComponent extends TailwindControl {
    protected widget = 'textarea';
    protected override fieldBase = 'textarea';
}
