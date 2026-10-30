import { Component, computed, input } from '@angular/core';

/**
 * "IAM UI by INDICE" brand: a shield with a keyhole and a status dot, followed by the wordmark. It mirrors
 * `assets/images/branding/logo.svg` as a single SVG so the layout never shifts, but takes its colors from theme tokens
 * (`--color-primary`, `--color-primary-content`, `--color-accent`, `--color-accent-content`) and the text from
 * `currentColor`, so it follows the active theme and the surface it sits on.
 */
@Component({
  selector: 'app-logo',
  host: { class: 'inline-flex' },
  template: `
    <svg viewBox="0 0 114 32" [style.width.px]="width()" [style.height.px]="size()" role="img" aria-label="IAM UI by INDICE"
      style="font-family: inherit; overflow: visible">
      <path d="M15 2.6 4.6 6.4a1.5 1.5 0 0 0-1 1.4v7c0 6.9 4.6 12.2 10.9 14.6a1.5 1.5 0 0 0 1 0c6.3-2.4 10.9-7.7 10.9-14.6v-7a1.5 1.5 0 0 0-1-1.4L15 2.6Z"
        style="fill: var(--color-primary)" />
      <circle cx="15" cy="13.2" r="3.6" style="fill: var(--color-primary-content)" />
      <path d="M13.3 15.4h3.4l1.1 6.4h-5.6Z" style="fill: var(--color-primary-content)" />
      <circle cx="25.5" cy="7" r="4.5" stroke-width="2" [style.fill]="'var(--color-accent)'" [style.stroke]="ring()" />
      <text x="38" y="19.5" font-size="18" font-weight="700" fill="currentColor" letter-spacing="-0.4">IAM</text>
      <rect x="78" y="6" width="22" height="15" rx="4" style="fill: var(--color-accent)" />
      <text x="89" y="17.5" text-anchor="middle" font-size="10" font-weight="800" letter-spacing="0.3"
        style="fill: var(--color-accent-content)">UI</text>
      <text x="38.5" y="28.5" font-size="6.5" font-weight="500" fill="currentColor" fill-opacity="0.55" letter-spacing="0.4">by <tspan font-weight="700" letter-spacing="0.9">INDICE</tspan></text>
    </svg>
  `
})
export class LogoComponent {
  /** Height of the logo in pixels; the width follows the 114:32 aspect ratio of the artwork. */
  public readonly size = input(32);
  /** Color of the ring around the status dot; match it to the surface the logo sits on. */
  public readonly ring = input('var(--color-base-100)');
  protected readonly width = computed(() => (this.size() * 114) / 32);
}
