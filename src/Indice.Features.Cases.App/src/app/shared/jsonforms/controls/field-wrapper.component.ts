import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TailwindControl } from './tailwind-control';

/**
 * Label, field addons, read-only value, error and help text around a control.
 * Eager change detection: the control's fields are mutated by JSON Forms outside of signals.
 */
@Component({
    selector: 'app-jf-field',
    template: `
    @let c = control();
    <div [class]="c.wrapperClass">
      @if (c.showLabel && c.title) {
        <label [attr.for]="c.id" [class]="c.labelClass" [class.sr-only]="c.notitle" [innerHTML]="c.title"></label>
      }
      @if (!c.showEditor) {
        <p class="min-h-10 rounded-field border border-base-300 bg-base-200/50 px-3 py-2 text-sm break-words whitespace-pre-line"
          [class]="c.readonlyText ? '' : 'text-base-content/50'">{{ c.readonlyText || '—' }}</p>
      } @else if (addonLeft() || addonRight()) {
        <div class="join w-full">
          @if (addonLeft()) {
            <span class="join-item flex items-center border border-base-300 bg-base-200 px-3 text-sm" [innerHTML]="addonLeft()"></span>
          }
          <div class="join-item flex-1"><ng-content /></div>
          @if (addonRight()) {
            <span class="join-item flex items-center border border-base-300 bg-base-200 px-3 text-sm" [innerHTML]="addonRight()"></span>
          }
        </div>
      } @else {
        <ng-content />
      }
      @if (c.errorText) {
        <p class="text-xs text-error" role="alert" [innerHTML]="c.errorText"></p>
      } @else if (c.helpText) {
        <p class="text-xs text-base-content/60" [innerHTML]="c.helpText"></p>
      }
    </div>`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class FieldWrapperComponent {
    readonly control = input.required<TailwindControl>();

    protected addonLeft(): string | undefined {
        return this.control().options.fieldAddonLeft || this.control().options.prepend;
    }

    protected addonRight(): string | undefined {
        return this.control().options.fieldAddonRight || this.control().options.append;
    }
}
