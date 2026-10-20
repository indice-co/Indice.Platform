import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CaseFormControl, FieldKind } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

/** Boolean fields (ajsf `checkbox` widget: the title is drawn next to the box, not above it). */
@Component({
    selector: 'app-jf-checkbox',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <label class="jf-check" [attr.for]="id" [class]="options.itemLabelHtmlClass || ''">
          <input
            type="checkbox"
            [class]="fieldClass"
            [class.pointer-events-none]="readonly"
            [checked]="data === true"
            [id]="id"
            [attr.readonly]="readonly ? 'readonly' : null"
            [attr.aria-invalid]="invalid"
            [attr.aria-describedby]="describedBy"
            [disabled]="readonly"
            (change)="setValue($any($event.target).checked)">
          @if (title) {
            <span [style.display]="notitle ? 'none' : ''" [innerHTML]="title"></span>
          }
        </label>
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class CheckboxControlComponent extends CaseFormControl {
    protected widget = 'checkbox';
    protected override fieldKind: FieldKind = 'checkbox';
    override showLabel = false;
}
