import { Component, input } from '@angular/core';

/**
 * "Risk UI" brand: a shield carrying a risk gauge whose needle leans toward the high end. Colors come from theme tokens
 * (`--color-primary`, `--color-primary-content`, `--color-accent`), so the mark follows the active theme.
 */
@Component({
  selector: 'app-logo',
  host: { class: 'inline-flex items-center gap-2.5' },
  template: `
    <svg viewBox="0 0 32 32" [style.width.px]="size()" [style.height.px]="size()" aria-hidden="true">
      <path d="M16 2.5 27.5 6.6V15c0 7.1-4.9 12-11.5 14.6C9.4 27 4.5 22.1 4.5 15V6.6Z" style="fill: var(--color-primary)" />
      <path d="M10 19.5a6 6 0 0 1 12 0" fill="none" stroke-width="2" stroke-linecap="round" style="stroke: var(--color-primary-content)" />
      <path d="m16 19.5 3.6-4.4" fill="none" stroke-width="2.2" stroke-linecap="round" style="stroke: var(--color-accent)" />
      <circle cx="16" cy="19.5" r="1.7" style="fill: var(--color-primary-content)" />
    </svg>
    @if (wordmark()) {
      <span class="flex items-baseline gap-1.5 leading-none">
        <span class="text-lg font-bold tracking-tight">Risk</span>
        <span class="rounded-md bg-accent px-1.5 py-0.5 text-[0.65rem] font-bold text-accent-content">UI</span>
      </span>
    }
  `
})
export class LogoComponent {
  public readonly size = input(32);
  public readonly wordmark = input(true);
}
