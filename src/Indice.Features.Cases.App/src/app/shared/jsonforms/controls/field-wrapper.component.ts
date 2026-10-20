import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CaseFormControl } from './case-form-control';

/** Label above, the projected field (with optional ajsf addons) and the help / error line below. */
@Component({
    selector: 'app-jf-field',
    template: `
    <div [class]="control().wrapperClass">
      @if (control().showLabel && control().title) {
        <label
          [attr.for]="control().id"
          [class]="control().labelClass"
          [class.sr-only]="control().notitle"
          [innerHTML]="control().title"></label>
      }
      <div [class.jf-input-group]="addonLeft || addonRight" [class.input-group]="addonLeft || addonRight">
        @if (addonLeft) {
          <span class="jf-addon input-group-addon" [innerHTML]="addonLeft"></span>
        }
        <ng-content />
        @if (addonRight) {
          <span class="jf-addon input-group-addon" [innerHTML]="addonRight"></span>
        }
      </div>
      @if (control().helpBlock) {
        <p class="jf-help text-xs" [id]="control().helpId" [class]="control().invalid ? 'text-error' : 'text-quiet'"
           [attr.role]="control().invalid ? 'alert' : null" [innerHTML]="control().helpBlock"></p>
      }
    </div>`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class FieldWrapperComponent {
    public readonly control = input.required<CaseFormControl>();

    get addonLeft(): string | undefined {
        return this.control().options.fieldAddonLeft || this.control().options.prepend;
    }

    get addonRight(): string | undefined {
        return this.control().options.fieldAddonRight || this.control().options.append;
    }
}
