import { Component, input } from '@angular/core';

/**
 * "Cases UI" brand: a case folder carrying a highlighter check mark. Colors come from theme tokens
 * (`--color-primary`, `--app-highlight`), so the mark follows the active theme.
 */
@Component({
  selector: 'app-logo',
  host: { class: 'inline-flex items-center gap-2.5' },
  template: `
    <svg viewBox="0 0 32 32" [style.width.px]="size()" [style.height.px]="size()" aria-hidden="true">
      <path d="M6 8.5A2.5 2.5 0 0 1 8.5 6h5.2l2.4 2.6h7.4a2.5 2.5 0 0 1 2.5 2.5V20H6Z"
        style="fill: color-mix(in oklab, var(--color-primary) 55%, black)" />
      <path d="M3.5 13.5A2.5 2.5 0 0 1 6 11h20a2.5 2.5 0 0 1 2.5 2.5v9A3.5 3.5 0 0 1 25 26H7a3.5 3.5 0 0 1-3.5-3.5Z"
        style="fill: var(--color-primary)" />
      <path d="M10.5 18.6l3.4 3.3 7.6-7.4" fill="none" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round"
        style="stroke: var(--app-highlight)" />
    </svg>
    @if (wordmark()) {
      <span class="flex items-baseline gap-1.5 leading-none">
        <span class="text-lg font-bold tracking-tight">Cases</span>
        <span class="rounded px-1 py-0.5 text-[0.65rem] font-semibold"
          style="background: var(--app-highlight); color: var(--color-accent-content)">UI</span>
      </span>
    }
  `
})
export class LogoComponent {
  public readonly size = input(32);
  public readonly wordmark = input(true);
}
