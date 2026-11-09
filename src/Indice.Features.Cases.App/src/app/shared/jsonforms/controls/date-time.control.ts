import { ChangeDetectionStrategy, Component } from '@angular/core';
import moment from 'moment';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

const INPUT_DATE_TIME = 'YYYY-MM-DDTHH:mm';

/**
 * `format: date-time` and `format: time` fields. The browser inputs give local, zone-less values, so they are converted to what
 * the formats validate: a date-time is stored as ISO 8601 with the local offset (`2026-10-21T14:30:00+03:00`), a time as `HH:mm:ss`.
 */
@Component({
    selector: 'app-jf-date-time',
    imports: [FieldWrapperComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (showEditor) {
          <input
            [type]="isTime ? 'time' : 'datetime-local'"
            [class]="fieldClass"
            [attr.required]="options.required"
            [id]="id"
            [value]="inputValue"
            (change)="onInput($any($event.target).value)">
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class DateTimeControlComponent extends TailwindControl {
    protected widget = 'date-time';
    isTime = false;
    inputValue = '';

    override get readonlyText(): string {
        return formatDateTimeValue(this.data, this.isTime ? 'time' : 'date-time');
    }

    protected override onStateChange(): void {
        this.isTime = this.options.format === 'time';
        this.widget = this.isTime ? 'time' : 'date-time';
        if (this.data === undefined || this.data === null || this.data === '') {
            this.inputValue = '';
        } else if (this.isTime) {
            this.inputValue = `${this.data}`.substring(0, 5);
        } else {
            const value = moment.parseZone(this.data, moment.ISO_8601);
            this.inputValue = value.isValid() ? value.local().format(INPUT_DATE_TIME) : '';
        }
    }

    onInput(value: string): void {
        if (!value) {
            this.setValue(undefined);
        } else if (this.isTime) {
            this.setValue(value.length === 5 ? `${value}:00` : value);
        } else {
            this.setValue(moment(value, INPUT_DATE_TIME).format());
        }
    }
}

/** A stored `date-time` (`DD/MM/YYYY HH:mm`, local time) or `time` (`HH:mm`) value for display. */
export function formatDateTimeValue(value: any, format: 'date-time' | 'time'): string {
    if (value === undefined || value === null || value === '') {
        return '';
    }
    if (format === 'time') {
        return /^\d\d:\d\d/.test(`${value}`) ? `${value}`.substring(0, 5) : `${value}`;
    }
    const date = moment.parseZone(value, moment.ISO_8601);
    return date.isValid() ? date.local().format('DD/MM/YYYY HH:mm') : `${value}`;
}
