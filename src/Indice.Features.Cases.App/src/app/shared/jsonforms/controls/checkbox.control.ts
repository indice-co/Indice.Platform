import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

/**
 * Boolean fields (ajsf built-in `checkbox` widget: the title is drawn next to the box, not above it); a switch with `options.toggle`.
 * Read-only boxes stay full colour (not `disabled`) so the answer remains legible; they just ignore input.
 */
@Component({
    selector: 'app-jf-checkbox',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <label [attr.for]="id" class="label w-fit gap-3 py-1 text-sm text-base-content whitespace-normal"
          [class]="options.itemLabelHtmlClass || ''" [class.cursor-pointer]="!readonly">
          <input
            type="checkbox"
            [class]="(options.toggle ? 'toggle toggle-sm toggle-primary ' : 'checkbox checkbox-sm checkbox-primary ') + (options.fieldHtmlClass || '')"
            [class.pointer-events-none]="readonly"
            [checked]="data === true"
            [id]="id"
            [attr.aria-readonly]="readonly"
            [attr.tabindex]="readonly ? -1 : null"
            (click)="onClick($event)"
            (change)="setValue($any($event.target).checked)">
          @if (title) {
            <span [class.sr-only]="notitle" [innerHTML]="title"></span>
          }
        </label>
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class CheckboxControlComponent extends TailwindControl {
    protected widget = 'checkbox';
    override showLabel = false;
    override readonlyAsText = false;

    /** Read-only boxes ignore clicks. Not an inline `readonly && ...`: a handler that evaluates to `false` makes Angular cancel the click. */
    onClick(event: Event): void {
        if (this.readonly) {
            event.preventDefault();
        }
    }
}
