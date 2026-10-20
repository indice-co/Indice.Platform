import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { CaseFormControl } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

/** ajsf `href` widget: a text input, shown as a link when read-only. */
@Component({
    selector: 'app-jf-href',
    imports: [FieldWrapperComponent, NgIcon],
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
            [attr.aria-invalid]="invalid"
            [attr.aria-describedby]="describedBy"
            [id]="id"
            [value]="data ?? ''"
            (input)="setValue($any($event.target).value)">
        } @else {
          <div class="jf-static jf-readonly-box">
            <a [href]="data" class="link link-primary inline-flex items-center gap-1.5 break-all" target="_blank" rel="noopener">
              {{ data }}
              @if (data) {
                <ng-icon name="lucideExternalLink" size="0.875rem" class="shrink-0" />
              }
            </a>
          </div>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class HrefControlComponent extends CaseFormControl {
    protected widget = 'href';
}
