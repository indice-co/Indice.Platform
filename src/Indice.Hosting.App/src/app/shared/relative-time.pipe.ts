import { Pipe, PipeTransform } from '@angular/core';

const formatter = new Intl.RelativeTimeFormat('en', { numeric: 'auto' });

/**
 * `{{ value | relativeTime }}` → "3 minutes ago" / "in 2 hours". Pure: the polling re-render hands
 * it fresh objects often enough to keep the text current without an impure pipe's per-CD cost.
 */
@Pipe({ name: 'relativeTime' })
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    if (value === null || value === undefined || value === '') {
      return '—';
    }
    const time = value instanceof Date ? value.getTime() : Date.parse(value);
    if (Number.isNaN(time)) {
      return '—';
    }
    const seconds = Math.round((time - Date.now()) / 1000);
    const abs = Math.abs(seconds);
    if (abs < 60) {
      return formatter.format(seconds, 'second');
    }
    if (abs < 3600) {
      return formatter.format(Math.round(seconds / 60), 'minute');
    }
    if (abs < 86400) {
      return formatter.format(Math.round(seconds / 3600), 'hour');
    }
    return formatter.format(Math.round(seconds / 86400), 'day');
  }
}
