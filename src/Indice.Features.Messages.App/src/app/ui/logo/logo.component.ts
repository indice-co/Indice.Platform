import { Component, input } from '@angular/core';

/**
 * "Messaging UI" brand: a speech bubble carrying a paper plane, with an unread-badge dot. Colors come from theme
 * tokens (`--color-primary`, `--color-accent`, `--color-base-100`), so the mark follows the active theme.
 */
@Component({
  selector: 'app-logo',
  host: { class: 'inline-flex items-center gap-2.5' },
  template: `
    <svg viewBox="0 0 32 32" [style.width.px]="size()" [style.height.px]="size()" aria-hidden="true">
      <path d="M7 4h16a5 5 0 0 1 5 5v10a5 5 0 0 1-5 5h-8.5L8 29v-5H7a5 5 0 0 1-5-5V9a5 5 0 0 1 5-5Z"
        style="fill: var(--color-primary)" />
      <path d="M8.5 13.6 21.8 8.4l-4.1 12.3-3.4-4.5Z" style="fill: var(--color-primary-content)" />
      <path d="m14.3 16.2 7.5-7.8" stroke-width="1.3" stroke-linecap="round" style="stroke: var(--color-primary)" />
      <circle cx="26" cy="6" r="4.5" stroke-width="2" style="fill: var(--color-accent); stroke: var(--app-sidebar-bg)" />
    </svg>
    @if (wordmark()) {
      <span class="flex items-baseline gap-1.5 leading-none">
        <span class="text-lg font-extrabold tracking-tight">Messaging</span>
        <span class="rounded-md bg-accent px-1.5 py-0.5 text-[0.65rem] font-extrabold text-accent-content">UI</span>
      </span>
    }
  `
})
export class LogoComponent {
  public readonly size = input(32);
  public readonly wordmark = input(true);
}
