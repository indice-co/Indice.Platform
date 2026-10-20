import { ChangeDetectionStrategy, Component } from '@angular/core';
import moment from 'moment';
import { CaseFormControl } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

/**
 * ajsf `date` widget, also used for every string field with `format: date`. The `jf-date` styles re-order the WebKit
 * date fields so the value reads day / month / year whatever the browser locale (see styles/jsonforms.css).
 */
@Component({
    selector: 'app-jf-date',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <input
          type="date"
          [class]="fieldClass + ' jf-date'"
          [attr.placeholder]="options.placeholder"
          [attr.required]="options.required"
          [attr.aria-invalid]="invalid"
          [attr.aria-describedby]="describedBy"
          [id]="id"
          [readonly]="readonly"
          [min]="min"
          [max]="max"
          [value]="data ?? ''"
          (input)="setValue($any($event.target).value)">
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class DateControlComponent extends CaseFormControl {
    protected widget = 'date';
    min = '';
    max = '';

    protected override onStateChange(): void {
        this.min = this.options.min !== undefined
            ? this.options.min
            : DateControlComponent.offsetToday(this.options.minDateYears ?? -100, this.options.minDateMonths ?? 0, this.options.minDateDays ?? 0);
        this.max = this.options.max
            ? this.options.max
            : DateControlComponent.offsetToday(this.options.maxDateYears ?? 100, this.options.maxDateMonths ?? 0, this.options.maxDateDays ?? 0);
    }

    private static offsetToday(years: number, months: number, days: number): string {
        return moment(new Date()).add(years, 'years').add(months, 'months').add(days, 'days').format('YYYY-MM-DD');
    }
}
