import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * "Workers UI by INDICE" brand: a rounded tile carrying a gear (the worker) and a status
 * dot, followed by the wordmark. It matches `favicon.svg` and `assets/images/branding/logo.svg` as a single SVG
 * so the layout never shifts, but takes its colors from theme tokens (`--color-primary`, `--color-primary-content`,
 * `--color-neutral`, `--color-neutral-content`) and the text from `currentColor`, so it follows the active theme
 * and the surface it sits on. `mark` renders the tile alone, for tight spots such as the collapsed rail.
 */
@Component({
  selector: 'app-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex shrink-0' },
  template: `
    <svg
      [attr.viewBox]="mark() ? '0 0 32 32' : '0 0 146 32'"
      [style.width.px]="width()"
      [style.height.px]="size()"
      role="img"
      aria-label="Workers UI by INDICE"
      style="font-family: inherit; overflow: visible"
    >
      <rect x="3" y="5" width="24" height="24" rx="7" style="fill: var(--color-primary)" />
      <path
        d="M13.62 11.47 13.78 9.7h2.44l.16 1.77 1.56.64 1.36-1.13 1.72 1.72-1.13 1.36.64 1.56 1.77.16v2.44l-1.77.16-.64 1.56 1.13 1.36-1.72 1.72-1.36-1.13-1.56.64-.16 1.77h-2.44l-.16-1.77-1.56-.64-1.36 1.13-1.72-1.72 1.13-1.36-.64-1.56-1.77-.16v-2.44l1.77-.16.64-1.56-1.13-1.36 1.72-1.72 1.36 1.13ZM15 14.25a2.75 2.75 0 1 0 0 5.5 2.75 2.75 0 1 0 0-5.5Z"
        fill-rule="evenodd"
        style="fill: var(--color-primary-content)"
      />
      <circle cx="26" cy="6" r="4.5" stroke-width="2" [style.fill]="'var(--color-neutral)'" [style.stroke]="ring()" />
      @if (!mark()) {
        <text x="38" y="19.5" font-size="18" font-weight="700" fill="currentColor" letter-spacing="-0.3">Workers</text>
        <rect x="111" y="6" width="22" height="15" rx="4" style="fill: var(--color-neutral)" />
        <text
          x="122"
          y="17.5"
          text-anchor="middle"
          font-size="10"
          font-weight="700"
          letter-spacing="0.3"
          style="fill: var(--color-neutral-content)"
        >UI</text>
        <text x="38.5" y="28.5" font-size="6.5" font-weight="500" fill="currentColor" fill-opacity="0.55" letter-spacing="0.4">by <tspan font-weight="700" letter-spacing="0.9">INDICE</tspan></text>
      }
    </svg>
  `,
})
export class LogoComponent {
  /** Height of the logo in pixels; the width follows the 146:32 aspect ratio of the artwork (1:1 for `mark`). */
  public readonly size = input(32);
  /** Color of the ring around the status dot; match it to the surface the logo sits on. */
  public readonly ring = input('var(--color-base-100)');
  /** Render the square mark only, without the wordmark. */
  public readonly mark = input(false);
  protected readonly width = computed(() => (this.mark() ? this.size() : (this.size() * 146) / 32));
}
