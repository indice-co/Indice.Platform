import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TailwindControl } from './tailwind-control';

/** Label, field addons and help block around a control, as the ajsf Tailwind framework drew them. */
@Component({
    selector: 'app-jf-field',
    template: `
    <div [class]="control.wrapperClass">
      @if (control.showLabel && control.title) {
        <label
          [attr.for]="control.id"
          [class]="control.labelClass"
          [class.sr-only]="control.notitle"
          [innerHTML]="control.title"></label>
      }
      <div [class.input-group]="addonLeft || addonRight">
        @if (addonLeft) {
          <span class="input-group-addon" [innerHTML]="addonLeft"></span>
        }
        <ng-content />
        @if (addonRight) {
          <span class="input-group-addon" [innerHTML]="addonRight"></span>
        }
      </div>
      @if (control.helpBlock) {
        <p class="text-red-500 text-xs italic" [innerHTML]="control.helpBlock"></p>
      }
    </div>`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class FieldWrapperComponent {
    @Input({ required: true }) control!: TailwindControl;

    get addonLeft(): string | undefined {
        return this.control.options.fieldAddonLeft || this.control.options.prepend;
    }

    get addonRight(): string | undefined {
        return this.control.options.fieldAddonRight || this.control.options.append;
    }
}
