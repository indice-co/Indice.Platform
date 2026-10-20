import { ChangeDetectionStrategy, Component } from '@angular/core';
import moment from 'moment';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

/** ajsf `date` widget, also used for every string field with `format: date`. */
@Component({
    selector: 'app-jf-date',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (showEditor) {
          <input
            type="date"
            [class]="fieldClass"
            [attr.placeholder]="options.placeholder"
            [attr.required]="options.required"
            [id]="id"
            [min]="min"
            [max]="max"
            [value]="data ?? ''"
            (input)="setValue($any($event.target).value)">
        }
      </app-jf-field>
    }`,
    // Shows the date as dd/mm/yyyy whatever the browser locale (https://stackoverflow.com/questions/7372038).
    styles: [`
    input[type="date"]::-webkit-datetime-edit,
    input[type="date"]::-webkit-inner-spin-button,
    input[type="date"]::-webkit-clear-button {
        color: transparent;
        position: relative;
    }
    input[type="date"]::-webkit-datetime-edit-year-field {
        position: absolute !important;
        padding: 2px;
        color: var(--color-base-content);
        left: 52px;
    }
    input[type="date"]::-webkit-datetime-edit-month-field {
        position: absolute !important;
        padding: 2px;
        color: var(--color-base-content);
        left: 28px;
    }
    input[type="date"]::-webkit-datetime-edit-day-field {
        position: absolute !important;
        color: var(--color-base-content);
        padding: 2px;
        left: 4px;
    }`],
    changeDetection: ChangeDetectionStrategy.Eager
})
export class DateControlComponent extends TailwindControl {
    protected widget = 'date';
    min = '';
    max = '';

    override get readonlyText(): string {
        const date = moment(this.data, 'YYYY-MM-DD', true);
        return date.isValid() ? date.format('DD/MM/YYYY') : super.readonlyText;
    }

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
