import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TailwindControl } from './tailwind-control';

/** Boolean fields (ajsf built-in `checkbox` widget: the title is drawn next to the box, not above it). */
@Component({
    selector: 'app-jf-checkbox',
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <label [attr.for]="id" [class]="options.itemLabelHtmlClass || ''">
          <input
            type="checkbox"
            [class]="options.fieldHtmlClass || ''"
            [class.pointer-events-none]="readonly"
            [checked]="data === true"
            [id]="id"
            [attr.readonly]="readonly ? 'readonly' : null"
            [disabled]="readonly"
            (change)="setValue($any($event.target).checked)">
          @if (title) {
            <span [style.display]="notitle ? 'none' : ''" [innerHTML]="title"></span>
          }
        </label>
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class CheckboxControlComponent extends TailwindControl {
    protected widget = 'checkbox';
    override showLabel = false;
}
