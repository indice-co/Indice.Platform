import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

/** Number fields with `options.slider` (the schema must define `minimum`, `maximum` and `default`): a slider with its value beside it. */
@Component({
    selector: 'app-jf-range',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (showEditor) {
          <div class="flex items-center gap-3">
            <input
              type="range"
              class="range range-sm range-primary flex-1"
              [class]="options.fieldHtmlClass || ''"
              [id]="id"
              [min]="options.minimum"
              [max]="options.maximum"
              [step]="step"
              [value]="data ?? options.default"
              (input)="setValue(+$any($event.target).value)">
            <output class="min-w-12 text-right text-sm font-medium tabular-nums" [attr.for]="id">{{ data ?? options.default }}</output>
          </div>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class RangeControlComponent extends TailwindControl {
    protected widget = 'range';
    protected override fieldBase = 'range';

    get step(): number | string {
        return this.options.multipleOf ?? (this.scopedSchema?.type === 'integer' ? 1 : 'any');
    }
}
