import { Component, LOCALE_ID, booleanAttribute, computed, inject, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

/** Shared building blocks of the sign in and activity log views. */

/** Browser icon (`assets/img/user-agent`) for a user agent family. */
export function userAgentIcon(family: string | null | undefined): string {
  const value = family?.toLowerCase() ?? '';
  const known = ['firefox', 'edge', 'chrome', 'brave', 'opera', 'tor', 'postman', 'safari'];
  let name = known.find(k => value.includes(k)) ?? (value === 'ie' ? 'ie' : value.includes('samsung') ? 'samsung-internet' : 'generic-browser');
  // chromium based browsers report "chrome" too; edge/brave/opera were matched before it
  if (value.includes('mobile') && name === 'generic-browser') {
    name = 'generic-phone';
  }
  return `assets/img/user-agent/${name}.svg`;
}

/** Operating system icon (`assets/img/os`) for a device platform. */
export function osIcon(platform: string | null | undefined): string {
  const value = platform?.toLowerCase() ?? '';
  const known = ['android', 'ios', 'linux', 'macos', 'ubuntu', 'windows'];
  const name = known.find(k => value === k) ?? (!value || value === 'none' ? 'none' : 'other');
  return `assets/img/os/${name}.svg`;
}

const IDPS = ['apple', 'facebook', 'github', 'google', 'govgr', 'microsoft', 'nbg', 'piraeus'];

/** External identity provider logo (`assets/img/idps`), or undefined for local / unknown providers. */
export function idpIcon(provider: string | null | undefined): string | undefined {
  const value = provider?.toLowerCase();
  if (!value || value === 'local') {
    return undefined;
  }
  const match = IDPS.find(k => value === k);
  if (!match) {
    return undefined;
  }
  const file = { apple: 'Apple', facebook: 'Facebook', github: 'Github', google: 'Google', govgr: 'GovGr', microsoft: 'Microsoft', nbg: 'Nbg', piraeus: 'Piraeus' }[match];
  return `assets/img/idps/${file}.svg`;
}

/** "5 min ago" style text for a past date, in the given locale. */
export function relativeTime(date: Date | string | null | undefined, locale = 'en', now = Date.now()): string {
  if (!date) {
    return '';
  }
  const seconds = Math.round((now - new Date(date).getTime()) / 1000);
  const format = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });
  const steps: [Intl.RelativeTimeFormatUnit, number][] = [['second', 60], ['minute', 60], ['hour', 24], ['day', 30], ['month', 12], ['year', Infinity]];
  let value = seconds;
  for (const [unit, size] of steps) {
    if (Math.abs(value) < size) {
      return format.format(-Math.round(value), unit);
    }
    value /= size;
  }
  return '';
}

/** Translation keys of the sign in types. */
const SIGN_IN_TYPE_LABELS: Record<string, string> = {
  Interactive: 'SignInLogs.SignInTypes.Interactive',
  NonInteractive: 'SignInLogs.SignInTypes.NonInteractive'
};

/** Translation keys of the sign in warnings. */
const SIGN_IN_WARNING_LABELS: Record<string, string> = {
  ImpossibleTravel: 'SignInLogs.Warnings.ImpossibleTravel'
};

/** Translation key of a sign in type (unknown values are returned as is). */
export function signInTypeLabel(type: string | null | undefined): string {
  return type ? SIGN_IN_TYPE_LABELS[type] ?? type : '';
}

/** Translation key of a sign in warning (unknown values are returned as is). */
export function signInWarningLabel(warning: string | null | undefined): string {
  return warning ? SIGN_IN_WARNING_LABELS[warning] ?? warning : '';
}

/** Converts a `from` / `to` query param (ISO date or date-time) into the `yyyy-MM-dd` value of a date input. */
export function toDateInput(value: string | null | undefined): string {
  if (!value) {
    return '';
  }
  const date = new Date(value);
  if (isNaN(date.getTime())) {
    return '';
  }
  return /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : date.toISOString().slice(0, 10);
}

/** Parses a `from` / `to` query param into a Date for the API. */
export function toDate(value: string | null | undefined): Date | undefined {
  if (!value) {
    return undefined;
  }
  const date = new Date(value);
  return isNaN(date.getTime()) ? undefined : date;
}

/** Two line timestamp: absolute date and time with a relative hint. */
@Component({
  selector: 'app-log-time',
  imports: [DatePipe],
  host: { class: 'flex flex-col leading-tight whitespace-nowrap' },
  template: `
    <span class="tabular text-sm">{{ value() | date: 'dd/MM/yyyy HH:mm:ss' }}</span>
    <span class="text-xs text-base-content/55">{{ relative() }}</span>
  `
})
export class LogTimeComponent {
  public readonly value = input<Date | undefined>();
  private readonly _locale = inject(LOCALE_ID);
  protected readonly relative = computed(() => relativeTime(this.value(), this._locale));
}

/** Browser and operating system icons followed by the device display name. */
@Component({
  selector: 'app-log-device',
  imports: [TranslatePipe],
  host: { class: 'inline-flex min-w-0 items-center gap-1.5' },
  template: `
    @if (displayName() || userAgentFamily() || platform()) {
      <img class="size-4 shrink-0" [src]="browser()" [alt]="userAgentFamily() || ('Logs.Device.Browser' | translate)" [title]="userAgentFamily() || ('Logs.Device.UnknownBrowser' | translate)" />
      <img class="size-4 shrink-0 dark:invert" [src]="os()" [alt]="platform() || ('Logs.Device.Os' | translate)" [title]="platform() || ('Logs.Device.UnknownPlatform' | translate)" />
      <span class="truncate text-sm" [title]="displayName()">{{ displayName() || '—' }}</span>
    } @else {
      <span class="text-base-content/40">—</span>
    }
  `
})
export class LogDeviceComponent {
  public readonly displayName = input<string | null | undefined>();
  public readonly userAgentFamily = input<string | null | undefined>();
  public readonly platform = input<string | null | undefined>();
  protected readonly browser = computed(() => userAgentIcon(this.userAgentFamily()));
  protected readonly os = computed(() => osIcon(this.platform()));
}

/** IP address with the country flag of its location. */
@Component({
  selector: 'app-log-ip',
  host: { class: 'inline-flex min-w-0 max-w-full items-center gap-2', '[class.whitespace-nowrap]': 'compact()' },
  template: `
    @if (country()) {
      <span class="flag shrink-0" [class]="'flag:' + country()" [title]="location() || country()"></span>
    }
    @if (ip()) {
      <span class="min-w-0 font-mono text-xs" [class.break-all]="!compact()" [title]="compact() ? ip() + (location() ? ' · ' + location() : '') : (location() || '')">{{ shown() }}</span>
    } @else {
      <span class="text-base-content/40">—</span>
    }
  `
})
export class LogIpComponent {
  public readonly ip = input<string | null | undefined>();
  public readonly country = input<string | null | undefined>();
  public readonly location = input<string | null | undefined>();
  /** Shortens IPv6 addresses to their first four groups; the full address is shown on hover. */
  public readonly compact = input(false, { transform: booleanAttribute });
  protected readonly shown = computed(() => {
    const ip = this.ip() ?? '';
    const groups = ip.split(':');
    return this.compact() && groups.length > 4 ? `${groups.slice(0, 4).join(':')}:…` : ip;
  });
}

/** A label and value pair in a log entry. */
@Component({
  selector: 'app-log-field',
  host: { class: 'grid gap-1 py-2.5 sm:grid-cols-[11rem_1fr] sm:gap-4' },
  template: `
    <dt class="text-sm font-medium text-base-content/60">{{ label() }}</dt>
    <dd class="min-w-0 text-sm break-words"><ng-content /></dd>
  `
})
export class LogFieldComponent {
  public readonly label = input.required<string>();
}
