import { Component, input } from '@angular/core';

/**
 * The Cases UI mark: a case folder inside a double-ruled stamp impression, the same device the app uses to show a
 * case's status. Drawn in `currentColor` so it follows the theme; the wordmark sits beside it unless `compact`.
 */
@Component({
  selector: 'app-logo',
  template: `
    <svg class="shrink-0 text-secondary" [attr.width]="size()" [attr.height]="size()" viewBox="0 0 32 32" fill="none" aria-hidden="true">
      <g transform="rotate(-6 16 16)">
        <rect x="2.75" y="2.75" width="26.5" height="26.5" rx="6.5" stroke="currentColor" stroke-width="2.5" />
        <rect x="6.6" y="6.6" width="18.8" height="18.8" rx="3.4" stroke="currentColor" stroke-width="1" opacity=".7" />
        <path fill="currentColor"
              d="M9.5 12.2c0-.83.67-1.5 1.5-1.5h3.3c.4 0 .78.16 1.06.44l1.3 1.3c.14.14.33.22.53.22h3.81c.83 0 1.5.67 1.5 1.5v7.04c0 .83-.67 1.5-1.5 1.5H11c-.83 0-1.5-.67-1.5-1.5z" />
      </g>
    </svg>
    @if (!compact()) {
      <span class="text-[1.0625rem] leading-none tracking-[-0.01em]">
        <span class="font-semibold">Cases</span><span class="ml-[0.2em] font-normal text-secondary">UI</span>
      </span>
    }
  `,
  host: { class: 'inline-flex items-center gap-2.5', 'role': 'img', 'aria-label': 'Cases UI' }
})
export class LogoComponent {
  public readonly compact = input(false);
  public readonly size = input(26);
}
