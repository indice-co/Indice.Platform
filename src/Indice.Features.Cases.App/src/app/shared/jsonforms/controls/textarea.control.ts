import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CaseFormControl, FieldKind } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

/** ajsf `textarea` widget. */
@Component({
    selector: 'app-jf-textarea',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <textarea
          [class]="fieldClass"
          [rows]="options.rows || 4"
          [attr.maxlength]="options.maxLength"
          [attr.minlength]="options.minLength"
          [attr.placeholder]="options.placeholder"
          [attr.required]="options.required"
          [attr.aria-invalid]="invalid"
          [attr.aria-describedby]="describedBy"
          [id]="id"
          [readonly]="readonly"
          [value]="data ?? ''"
          (input)="setValue($any($event.target).value)"></textarea>
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class TextareaControlComponent extends CaseFormControl {
    protected widget = 'textarea';
    protected override fieldKind: FieldKind = 'textarea';
}
