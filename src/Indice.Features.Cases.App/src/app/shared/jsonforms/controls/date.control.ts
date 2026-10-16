import { ChangeDetectionStrategy, Component } from '@angular/core';
import moment from 'moment';
import { TailwindControl } from './tailwind-control';

/** ajsf `date` widget, also used for every string field with `format: date`. */
@Component({
    selector: 'app-jf-date',
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <input
          type="date"
          [class]="fieldClass"
          [attr.placeholder]="options.placeholder"
          [attr.required]="options.required"
          [id]="id"
          [readonly]="readonly"
          [min]="min"
          [max]="max"
          [value]="data ?? ''"
          (input)="setValue($any($event.target).value)">
      </app-jf-field>
    }`,
    // https://stackoverflow.com/questions/7372038/is-there-any-way-to-change-input-type-date-format
    styles: [`
    input[type="date"]::-webkit-datetime-edit,
    input[type="date"]::-webkit-inner-spin-button,
    input[type="date"]::-webkit-clear-button {
        color: #fff;
        position: relative;
    }
    input[type="date"]::-webkit-datetime-edit-year-field {
        position: absolute !important;
        padding: 2px;
        color: #000;
        left: 52px;
    }
    input[type="date"]::-webkit-datetime-edit-month-field {
        position: absolute !important;
        padding: 2px;
        color: #000;
        left: 28px;
    }
    input[type="date"]::-webkit-datetime-edit-day-field {
        position: absolute !important;
        color: #000;
        padding: 2px;
        left: 4px;
    }`],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class DateControlComponent extends TailwindControl {
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
