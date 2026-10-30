import { Component, computed, input } from '@angular/core';

/**
 * "Messaging UI by INDICE" brand: a speech bubble carrying a paper plane and an unread-badge dot, followed by the
 * wordmark. It mirrors `assets/images/branding/logo.svg` as a single SVG so the layout never shifts, but takes its
 * colors from theme tokens (`--color-primary`, `--color-accent`, `--color-accent-content`) and the text from
 * `currentColor`, so it follows the active theme and the surface it sits on.
 */
@Component({
  selector: 'app-logo',
  host: { class: 'inline-flex' },
  template: `
    <svg viewBox="0 0 176 32" [style.width.px]="width()" [style.height.px]="size()" role="img" aria-label="Messaging UI by INDICE"
      style="font-family: inherit; overflow: visible">
      <path d="M7 4h16a5 5 0 0 1 5 5v10a5 5 0 0 1-5 5h-8.5L8 29v-5H7a5 5 0 0 1-5-5V9a5 5 0 0 1 5-5Z"
        style="fill: var(--color-primary)" />
      <path d="M8.5 13.6 21.8 8.4l-4.1 12.3-3.4-4.5Z" style="fill: var(--color-primary-content)" />
      <path d="m14.3 16.2 7.5-7.8" stroke-width="1.3" stroke-linecap="round" style="stroke: var(--color-primary)" />
      <circle cx="26" cy="6" r="4.5" stroke-width="2" style="fill: var(--color-accent); stroke: var(--app-sidebar-bg)" />
          <text x="38" y="19" font-size="18" font-weight="800" fill="currentColor" letter-spacing="-0.4">Messaging</text>
          <rect x="139" y="5.5" width="24" height="15" rx="4" style="fill: var(--color-accent)" />
          <text x="151" y="17" text-anchor="middle" font-size="10" font-weight="800"
            style="fill: var(--color-accent-content)">UI</text>
          <text x="38.5" y="28.5" font-size="6.5" font-weight="500" fill="currentColor" fill-opacity="0.55" letter-spacing="0.4">by <tspan font-weight="800" letter-spacing="0.9">INDICE</tspan></text>
        </svg>
      `
    })
    export class LogoComponent {
      /** Height of the logo in pixels; the width follows the 176:32 aspect ratio of the artwork. */
      public readonly size = input(32);
      protected readonly width = computed(() => (this.size() * 176) / 32);
    }
