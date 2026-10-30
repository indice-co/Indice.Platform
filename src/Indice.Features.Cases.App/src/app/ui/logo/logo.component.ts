import { Component, computed, input } from '@angular/core';

/**
 * "Cases UI by INDICE" brand: a case folder carrying a highlighter check mark and a status dot, followed by the
 * wordmark. It mirrors `assets/images/branding/logo.svg` as a single SVG so the layout never shifts, but takes its
 * colors from theme tokens (`--color-primary`, `--app-highlight`, `--color-accent`, `--color-accent-content`) and the
 * text from `currentColor`, so it follows the active theme and the surface it sits on.
 */
@Component({
  selector: 'app-logo',
  host: { class: 'inline-flex' },
  template: `
    <svg viewBox="0 0 132 32" [style.width.px]="width()" [style.height.px]="size()" role="img" aria-label="Cases UI by INDICE"
      style="font-family: inherit; overflow: visible">
      <path d="M6 8.5A2.5 2.5 0 0 1 8.5 6h5.2l2.4 2.6h7.4a2.5 2.5 0 0 1 2.5 2.5V20H6Z"
        style="fill: color-mix(in oklab, var(--color-primary) 55%, black)" />
      <path d="M3.5 13.5A2.5 2.5 0 0 1 6 11h20a2.5 2.5 0 0 1 2.5 2.5v9A3.5 3.5 0 0 1 25 26H7a3.5 3.5 0 0 1-3.5-3.5Z"
        style="fill: var(--color-primary)" />
      <path d="M10.5 18.6l3.4 3.3 7.6-7.4" fill="none" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round"
        style="stroke: var(--app-highlight)" />
      <circle cx="26" cy="6" r="4.5" stroke-width="2" [style.fill]="'var(--color-accent)'" [style.stroke]="ring()" />
      <text x="38" y="19.5" font-size="18" font-weight="700" fill="currentColor" letter-spacing="-0.3">Cases</text>
      <rect x="96" y="6" width="22" height="15" rx="3" style="fill: var(--app-highlight)" />
      <text x="107" y="17.5" text-anchor="middle" font-size="10" font-weight="700"
        style="fill: var(--color-accent-content)">UI</text>
      <text x="38.5" y="28.5" font-size="6.5" font-weight="500" fill="currentColor" fill-opacity="0.55" letter-spacing="0.4">by <tspan font-weight="700" letter-spacing="0.9">INDICE</tspan></text>
    </svg>
  `
})
export class LogoComponent {
  /** Height of the logo in pixels; the width follows the 132:32 aspect ratio of the artwork. */
  public readonly size = input(32);
  /** Color of the ring around the status dot; match it to the surface the logo sits on. */
  public readonly ring = input('var(--color-base-100)');
  protected readonly width = computed(() => (this.size() * 132) / 32);
}
