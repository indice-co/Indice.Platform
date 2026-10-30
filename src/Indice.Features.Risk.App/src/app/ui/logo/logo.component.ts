import { Component, computed, input } from '@angular/core';

/**
 * "Risk UI by INDICE" brand: a rounded warning diamond carrying a pulse line and an alert dot, followed by the wordmark.
 * It matches `favicon.svg` as a single SVG so the layout never shifts, but takes its colors from theme tokens
 * (`--color-primary`, `--color-primary-content`, `--color-accent`, `--color-accent-content`) and the text from
 * `currentColor`, so it follows the active theme and the surface it sits on.
 */
@Component({
  selector: 'app-logo',
  host: { class: 'inline-flex' },
  template: `
    <svg viewBox="0 0 112 32" [style.width.px]="width()" [style.height.px]="size()" role="img" aria-label="Risk UI by INDICE"
      style="font-family: inherit; overflow: visible">
      <rect x="6" y="7" width="18" height="18" rx="4.5" transform="rotate(45 15 16)" style="fill: var(--color-primary)" />
      <path d="M7 16.5h4l2-5 3.5 9.5 2.5-6.5 1.5 2H23" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
        style="stroke: var(--color-primary-content)" />
      <circle cx="26" cy="6" r="4.5" stroke-width="2" [style.fill]="'var(--color-accent)'" [style.stroke]="ring()" />
      <text x="38" y="19.5" font-size="18" font-weight="700" fill="currentColor" letter-spacing="-0.3">Risk</text>
      <rect x="77" y="6" width="22" height="15" rx="3" style="fill: var(--color-accent)" />
      <text x="88" y="17.5" text-anchor="middle" font-size="10" font-weight="700"
        style="fill: var(--color-accent-content)">UI</text>
      <text x="38.5" y="28.5" font-size="6.5" font-weight="500" fill="currentColor" fill-opacity="0.55" letter-spacing="0.4">by <tspan font-weight="700" letter-spacing="0.9">INDICE</tspan></text>
    </svg>
  `
})
export class LogoComponent {
  /** Height of the logo in pixels; the width follows the 112:32 aspect ratio of the artwork. */
  public readonly size = input(32);
  /** Color of the ring around the alert dot; match it to the surface the logo sits on. */
  public readonly ring = input('var(--color-base-100)');
  protected readonly width = computed(() => (this.size() * 112) / 32);
}
