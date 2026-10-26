import { Component, input } from '@angular/core';

/**
 * "Admin UI" brand: a shield with a keyhole and a status dot. Colors come from theme tokens (`--color-primary`,
 * `--color-primary-content`, `--color-accent`, `--app-sidebar-bg`), so the mark follows the active theme.
 */
@Component({
  selector: 'app-logo',
  host: { class: 'inline-flex items-center gap-2.5' },
  template: `
    <svg viewBox="0 0 32 32" [style.width.px]="size()" [style.height.px]="size()" aria-hidden="true">
      <path d="M15 2.6 4.6 6.4a1.5 1.5 0 0 0-1 1.4v7c0 6.9 4.6 12.2 10.9 14.6a1.5 1.5 0 0 0 1 0c6.3-2.4 10.9-7.7 10.9-14.6v-7a1.5 1.5 0 0 0-1-1.4L15 2.6Z"
        style="fill: var(--color-primary)" />
      <circle cx="15" cy="13.2" r="3.6" style="fill: var(--color-primary-content)" />
      <path d="M13.3 15.4h3.4l1.1 6.4h-5.6Z" style="fill: var(--color-primary-content)" />
      <circle cx="25.5" cy="7" r="4.5" stroke-width="2" [style.fill]="'var(--color-accent)'" [style.stroke]="ring()" />
    </svg>
    @if (wordmark()) {
      <span class="flex items-baseline gap-1.5 leading-none">
        <span class="text-lg font-bold tracking-tight">Admin</span>
        <span class="rounded-md bg-accent px-1.5 py-0.5 text-[0.65rem] font-extrabold tracking-wide text-accent-content">UI</span>
      </span>
    }
  `
})
export class LogoComponent {
  public readonly size = input(32);
  public readonly wordmark = input(true);
  /** Color of the ring around the status dot; match it to the surface the logo sits on. */
  public readonly ring = input('var(--color-base-100)');
}
